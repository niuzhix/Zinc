using System.Diagnostics;
using System.Text;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;

namespace Zinc.Core.Services;

public class ProgramService : IProgramService
{
    public async Task<CompileResult> CompileAsync(CompileOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CompileResult();

        try
        {
            var compilers = FindAllCompilers();
            if (compilers.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorType = CompileErrorType.CompilerNotFound;
                result.ErrorMessage = "未找到任何 C++ 编译器 (g++)，请安装 MinGW-w64 或 GCC。";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            string compilerPath = compilers.FirstOrDefault(c => c.IsDefault)?.Path ?? compilers[0].Path;
            result.CompilerPath = compilerPath;

            if (string.IsNullOrWhiteSpace(options.CodePath) || !File.Exists(options.CodePath))
            {
                result.IsSuccess = false;
                result.ErrorType = CompileErrorType.SourceFileNotFound;
                result.ErrorMessage = $"源文件不存在: {options.CodePath}";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            string outputPath = GetOutputPath(options.CodePath);
            string arguments = BuildCompileArguments(options, outputPath);
            result.FullCommand = $"\"{compilerPath}\" {arguments}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = compilerPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(options.CodePath) ?? string.Empty
                }
            };

            var tcs = new TaskCompletionSource<int>();
            process.Exited += (sender, args) => tcs.TrySetResult(process.ExitCode);
            process.EnableRaisingEvents = true;

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            int exitCode = await tcs.Task;

            await Task.WhenAll(outputTask, errorTask);

            result.ExitCode = exitCode;
            result.Output = await outputTask;
            result.Error = await errorTask;
            result.IsSuccess = exitCode == 0;

            if (!result.IsSuccess)
            {
                result.ErrorType = CompileErrorType.CompilationFailed;
                result.ErrorMessage = "编译失败，请检查代码错误。";
            }

            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            LogCompileResult(result);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorType = CompileErrorType.InternalError;
            result.ErrorMessage = $"编译内部错误: {ex.Message}";
            result.Output = ex.StackTrace ?? string.Empty;
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }

    public List<CompilerInfo> FindAllCompilers()
    {
        var compilers = new List<CompilerInfo>();
        var foundPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "g++",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);

            if (!string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();

                    if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                        trimmed = trimmed.Substring(1, trimmed.Length - 2);

                    if (!string.IsNullOrWhiteSpace(trimmed) && File.Exists(trimmed))
                    {
                        if (foundPaths.Add(trimmed))
                        {
                            compilers.Add(new CompilerInfo
                            {
                                Path = trimmed,
                                Version = GetCompilerVersion(trimmed),
                                IsDefault = compilers.Count == 0
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"查找编译器失败: {ex.Message}");
        }

        if (compilers.Count == 0)
        {
            var fallbackPaths = new[]
            {
                @"C:\mingw64\bin\g++.exe",
                @"C:\MinGW\bin\g++.exe",
                @"C:\Program Files\mingw-w64\bin\g++.exe",
                @"C:\msys64\mingw64\bin\g++.exe",
                @"C:\msys64\ucrt64\bin\g++.exe"
            };

            foreach (var path in fallbackPaths)
            {
                if (File.Exists(path) && foundPaths.Add(path))
                {
                    compilers.Add(new CompilerInfo
                    {
                        Path = path,
                        Version = GetCompilerVersion(path),
                        IsDefault = compilers.Count == 0
                    });
                }
            }
        }

        return compilers;
    }
    private string GetCompilerVersion(string compilerPath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = compilerPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            if (!string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    return lines[0].Trim();
                }
            }
        }
        catch {  }
        return "Unknown Version";
    }

    private string BuildCompileArguments(CompileOptions options, string outputPath)
    {
        var args = new StringBuilder();

        args.Append($"\"{options.CodePath}\"");

        args.Append($" -o \"{outputPath}\"");

        string standard = GetStandardString(options.StandardVersion);
        args.Append($" -std={standard}");

        if (options.enableO2)
            args.Append(" -O2");
        else
            args.Append(" -O0");

        if (options.enableGDB)
            args.Append(" -g");

        if (options.warningCheck)
            args.Append(" -Wall -Wextra -Wshadow -Wconversion -Wpedantic");

        if (options.overAddressCheck)
            args.Append(" -fsanitize=undefined -fsanitize=address");

        args.Append(" -pipe");
        args.Append(" -fno-omit-frame-pointer");

        return args.ToString();
    }

    private string GetOutputPath(string codePath)
    {
        return Path.ChangeExtension(codePath, ".exe");
    }

    private string GetStandardString(CppStandard standard)
    {
        return standard switch
        {
            CppStandard.Cpp98 => "c++98",
            CppStandard.Cpp03 => "c++03",
            CppStandard.Cpp11 => "c++11",
            CppStandard.Cpp14 => "c++14",
            CppStandard.Cpp17 => "c++17",
            CppStandard.Cpp20 => "c++20",
            CppStandard.Cpp23 => "c++23",
            CppStandard.Cpp26 => "c++26",
            _ => "c++17"
        };
    }
    private void LogCompileResult(CompileResult result)
    {
        var log = new StringBuilder();
        log.AppendLine("========== 编译日志 ==========");
        log.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        log.AppendLine($"编译器: {result.CompilerPath}");
        log.AppendLine($"命令: {result.FullCommand}");
        log.AppendLine($"退出码: {result.ExitCode}");
        log.AppendLine($"耗时: {result.ElapsedMilliseconds}ms");
        log.AppendLine($"状态: {(result.IsSuccess ? "✅ 成功" : "❌ 失败")}");

        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            log.AppendLine("--- 标准输出 ---");
            log.AppendLine(result.Output);
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            log.AppendLine("--- 错误输出 ---");
            log.AppendLine(result.Error);
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            log.AppendLine($"--- 错误信息 ---");
            log.AppendLine(result.ErrorMessage);
        }

        log.AppendLine("==============================");

        Console.WriteLine(log.ToString());
    }
}