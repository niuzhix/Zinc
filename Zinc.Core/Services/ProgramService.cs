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
    private const string UnknownVersion = "Unknown Version";

    private static readonly string[] WindowsCompilerNames = ["g++.exe", "clang++.exe"];
    private static readonly string[] UnixCompilerNames = ["g++", "clang++"];

    private static readonly string[] WindowsFallbackPaths =
    [
        @"C:\mingw64\bin\g++.exe",
        @"C:\MinGW\bin\g++.exe",
        @"C:\Program Files\mingw-w64\bin\g++.exe",
        @"C:\msys64\mingw64\bin\g++.exe",
        @"C:\msys64\ucrt64\bin\g++.exe",
    ];

    private static readonly string[] UnixFallbackPaths =
    [
        "/usr/bin/g++",
        "/usr/local/bin/g++",
        "/usr/bin/clang++",
        "/usr/local/bin/clang++",
    ];

    private Task<IReadOnlyList<CompilerInfo>>? _compilerDiscovery;

    public Task<IReadOnlyList<CompilerInfo>> FindAllCompilersAsync()
        => _compilerDiscovery ??= DiscoverCompilersAsync();

    public async Task<CompileResult> CompileAsync(CompileOptions options)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CompileResult();

        try
        {
            var compilers = await FindAllCompilersAsync();

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
                        Version = await GetCompilerVersionAsync(options.CompilerPath),
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
            string outputPath = CompileArtifacts.GetExecutablePath(fullSourcePath);
            string workingDir = Path.GetDirectoryName(fullSourcePath) ?? string.Empty;

            var arguments = BuildCompileArguments(options, outputPath);
            result.FullCommand = $"{compiler.Path} {string.Join(" ", arguments)}";

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
                TryKill(process);

                result.Output = await TryReadAsync(outputTask);
                result.Error = await TryReadAsync(errorTask);
                result.IsSuccess = false;
                result.ErrorType = CompileErrorType.InternalError;
                result.ErrorMessage = "编译超时";
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return result;
            }

            result.ExitCode = process.ExitCode;
            result.Output = await outputTask;
            result.Error = await errorTask;

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
            return result;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[ProgramService] 编译过程异常: {ex}");

            result.IsSuccess = false;
            result.ErrorType = CompileErrorType.InternalError;
            result.ErrorMessage = ex.Message;
            result.Output = ex.StackTrace ?? string.Empty;
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }

    private static async Task<IReadOnlyList<CompilerInfo>> DiscoverCompilersAsync()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = new List<string>();

        var compilerNames = OperatingSystem.IsWindows() ? WindowsCompilerNames : UnixCompilerNames;
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in compilerNames)
            {
                var fullPath = Path.Combine(dir, name);
                if (File.Exists(fullPath) && seen.Add(fullPath))
                {
                    paths.Add(fullPath);
                }
            }
        }

        if (paths.Count == 0)
        {
            var fallbackPaths = OperatingSystem.IsWindows() ? WindowsFallbackPaths : UnixFallbackPaths;
            foreach (var path in fallbackPaths)
            {
                if (File.Exists(path) && seen.Add(path))
                {
                    paths.Add(path);
                }
            }
        }

        var compilers = new List<CompilerInfo>(paths.Count);
        foreach (var path in paths)
        {
            compilers.Add(new CompilerInfo
            {
                Path = path,
                Version = await GetCompilerVersionAsync(path),
                IsDefault = compilers.Count == 0
            });
        }

        return compilers;
    }

    private static async Task<string> GetCompilerVersionAsync(string compilerPath)
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

            var outputTask = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();

            using var cts = new CancellationTokenSource(CompilerVersionTimeoutMs);
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return UnknownVersion;
            }

            var output = await outputTask;
            if (string.IsNullOrWhiteSpace(output))
                return UnknownVersion;

            var firstLine = output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            return firstLine?.Trim() ?? UnknownVersion;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[ProgramService] 读取编译器版本失败 ({compilerPath}): {ex.Message}");
            return UnknownVersion;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[ProgramService] 结束编译器进程失败: {ex.Message}");
        }
    }

    private static async Task<string> TryReadAsync(Task<string> readTask)
    {
        try
        {
            return await readTask;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static List<string> BuildCompileArguments(CompileOptions options, string outputPath)
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

    private static string GetStandardString(CppStandard standard)
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

        if (result.IsSuccess)
        {
            log.Append("[编译成功] ");
        }
        else
        {
            log.Append($"[{result.ErrorMessage}] ");

            if (!string.IsNullOrWhiteSpace(result.Error))
                log.Append(result.Error);
        }

        log.Append($"耗时: {result.ElapsedMilliseconds}ms");
        return log.ToString();
    }
}
