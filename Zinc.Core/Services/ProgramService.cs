using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;

namespace Zinc.Core.Services;

public class ProgramService : IProgramService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private const int CompilerVersionTimeoutMs = 2000;
    private const int CompileTimeoutMs = 30_000;

    public async Task<CompileResult> CompileAsync(CompileOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CompileResult();

        try
        {
            var compilers = FindAllCompilers();

            CompilerInfo? compiler = null;

            if (!string.IsNullOrWhiteSpace(options.CompilerPath) && File.Exists(options.CompilerPath))
            {
                compiler = compilers.FirstOrDefault(c =>
                    c.Path.Equals(options.CompilerPath, StringComparison.OrdinalIgnoreCase));

                if (compiler == null)
                {
                    compiler = new CompilerInfo
                    {
                        Path = options.CompilerPath,
                        Version = GetCompilerVersion(options.CompilerPath),
                        IsDefault = true
                    };
                }
            }

            if (compiler == null)
            {
                if (compilers.Count == 0)
                {
                    result.IsSuccess = false;
                    result.ErrorType = CompileErrorType.CompilerNotFound;
                    result.ErrorMessage = "未找到 C++ 编译器";
                    result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    return result;
                }

                compiler = compilers.FirstOrDefault(c => c.IsDefault) ?? compilers[0];
            }

            result.CompilerPath = compiler.Path;

            if (string.IsNullOrWhiteSpace(options.CodePath) || !File.Exists(options.CodePath))
            {
                result.IsSuccess = false;
                result.ErrorType = CompileErrorType.SourceFileNotFound;
                result.ErrorMessage = "源文件不存在";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            string fullSourcePath = Path.GetFullPath(options.CodePath);
            string outputPath = GetOutputPath(fullSourcePath);
            string workingDir = Path.GetDirectoryName(fullSourcePath) ?? string.Empty;

            var arguments = BuildCompileArguments(options, outputPath);
            result.FullCommand = $"{compiler.Path} {string.Join(" ", arguments)}";

            Console.WriteLine($"[OutputPath] {outputPath}");
            Console.WriteLine($"[FullCommand] {result.FullCommand}");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = compiler.Path,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDir,
                    StandardOutputEncoding = Utf8NoBom,
                    StandardErrorEncoding = Utf8NoBom
                }
            };

            foreach (var arg in arguments)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(CompileTimeoutMs);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                result.IsSuccess = false;
                result.ErrorType = CompileErrorType.InternalError;
                result.ErrorMessage = "编译超时";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.ExitCode = process.ExitCode;
            result.Output = await outputTask;
            result.Error = await errorTask;

            Console.WriteLine($"[stdout] {result.Output}");
            Console.WriteLine($"[stderr] {result.Error}");

            if (process.ExitCode == 0 && File.Exists(outputPath))
            {
                result.IsSuccess = true;
            }
            else if (process.ExitCode == 0)
            {
                result.IsSuccess = false;
                result.ErrorType = CompileErrorType.CompilationFailed;
                result.ErrorMessage = $"编译返回成功，但未找到产物: {outputPath}";
            }
            else
            {
                result.IsSuccess = false;
                result.ErrorType = CompileErrorType.CompilationFailed;
                result.ErrorMessage = "编译失败";
            }

            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            LogCompileResult(result);
            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorType = CompileErrorType.InternalError;
            result.ErrorMessage = ex.Message;
            result.Output = ex.StackTrace ?? string.Empty;
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }

    public List<CompilerInfo> FindAllCompilers()
    {
        var compilers = new List<CompilerInfo>();
        var foundPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathDirs = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var compilerNames = OperatingSystem.IsWindows()
            ? new[] { "g++.exe", "clang++.exe" }
            : new[] { "g++", "clang++" };

        foreach (var dir in pathDirs)
        {
            foreach (var name in compilerNames)
            {
                var fullPath = Path.Combine(dir, name);
                if (File.Exists(fullPath) && foundPaths.Add(fullPath))
                {
                    compilers.Add(new CompilerInfo
                    {
                        Path = fullPath,
                        Version = GetCompilerVersion(fullPath),
                        IsDefault = compilers.Count == 0
                    });
                }
            }
        }

        if (compilers.Count == 0)
        {
            string[] fallbackPaths;
            if (OperatingSystem.IsWindows())
            {
                fallbackPaths = new[]
                {
                    @"C:\mingw64\bin\g++.exe",
                    @"C:\MinGW\bin\g++.exe",
                    @"C:\Program Files\mingw-w64\bin\g++.exe",
                    @"C:\msys64\mingw64\bin\g++.exe",
                    @"C:\msys64\ucrt64\bin\g++.exe"
                };
            }
            else
            {
                fallbackPaths = new[]
                {
                    "/usr/bin/g++",
                    "/usr/local/bin/g++",
                    "/usr/bin/clang++",
                    "/usr/local/bin/clang++"
                };
            }

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
            using var process = new Process
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
            if (!process.WaitForExit(CompilerVersionTimeoutMs))
            {
                process.Kill();
                return "Unknown Version";
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                var firstLine = output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                return firstLine?.Trim() ?? "Unknown Version";
            }
        }
        catch
        {
        }
        return "Unknown Version";
    }

    private List<string> BuildCompileArguments(CompileOptions options, string outputPath)
    {
        var args = new List<string>
        {
            options.CodePath,
            "-o", outputPath,
            $"-std={GetStandardString(options.StandardVersion)}",
            options.enableO2 ? "-O2" : "-O0"
        };

        if (options.enableGDB)
            args.Add("-g");

        if (options.warningCheck)
        {
            args.Add("-Wall");
            args.Add("-Wextra");
            args.Add("-Wshadow");
            args.Add("-Wconversion");
            args.Add("-Wpedantic");
        }

        if (options.overAddressCheck)
        {
            args.Add("-fsanitize=undefined");
            args.Add("-fsanitize=address");
        }

        args.Add("-pipe");
        args.Add("-fno-omit-frame-pointer");

        return args;
    }

    private string GetOutputPath(string fullSourcePath)
    {
        return OperatingSystem.IsWindows()
            ? Path.ChangeExtension(fullSourcePath, ".exe")
            : Path.ChangeExtension(fullSourcePath, null);
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

    public string LogCompileResult(CompileResult result)
    {
        var log = new StringBuilder();
        log.Append($"[{DateTime.Now.ToLongTimeString()}] ");

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            log.Append($"[{result.ErrorMessage}] ");
            log.Append(result.Error);
        }
        else
        {
            log.Append("[编译成功] ");
        }

        log.Append($"耗时: {result.ElapsedMilliseconds}ms\n");
        return log.ToString();
    }
}