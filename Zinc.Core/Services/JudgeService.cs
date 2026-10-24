using System.Diagnostics;
using System.Text;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;

namespace Zinc.Core.Services;

public class JudgeService : IJudgeService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private const int KillWaitMilliseconds = 1000;

    private readonly IOutputComparer _outputComparer;
    private readonly IProcessMonitor _processMonitor;

    public JudgeService(IOutputComparer? outputComparer = null, IProcessMonitor? processMonitor = null)
    {
        _outputComparer = outputComparer ?? new OutputComparer();
        _processMonitor = processMonitor ?? new ProcessMonitor();
    }

    public async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, CancellationToken cancellationToken = default)
    {
        if (!ValidateOptions(options))
        {
            return new ExecutionResult { Result = JudgeResult.CE };
        }

        var result = new ExecutionResult
        {
            ExpectedOutput = options.ExpectedOutput,
            Result = JudgeResult.UKE
        };

        Process? process = null;
        try
        {
            process = CreateProcess(options);
            var stopwatch = Stopwatch.StartNew();

            process.Start();

            var outputTask = ReadStreamAsync(process.StandardOutput.BaseStream, Encoding.GetEncoding(options.OutputEncoding));
            var errorTask = ReadStreamAsync(process.StandardError.BaseStream, Encoding.GetEncoding(options.OutputEncoding));

            await WriteInputAsync(process, options.StandardInput, Encoding.GetEncoding(options.InputEncoding));

            var monitorTask = _processMonitor.MonitorAsync(
                process,
                options.MemoryLimitMB * 1024L * 1024L,
                cancellationToken);

            bool completed = await WaitForExitAsync(process, options.TimeLimitMs, cancellationToken);

            stopwatch.Stop();

            if (!completed)
            {
                KillProcess(process);
                result.Result = JudgeResult.TLE;
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }

            await Task.WhenAll(outputTask, errorTask);

            result.ExecutionTime = stopwatch.Elapsed;
            result.ExitCode = process.ExitCode;
            result.StandardOutput = outputTask.Result;
            result.ErrorOutput = errorTask.Result;

            var resourceUsage = await monitorTask;
            result.MemoryUsedBytes = resourceUsage.PeakMemoryBytes;

            if (options.MemoryLimitMB > 0 && resourceUsage.MemoryLimitExceeded)
            {
                result.Result = JudgeResult.MLE;
                return result;
            }

            if (process.ExitCode != 0)
            {
                result.Result = JudgeResult.RE;
                return result;
            }

            var compareResult = _outputComparer.Compare(result.StandardOutput, options.ExpectedOutput);
            if (compareResult.IsMatch)
            {
                result.Result = JudgeResult.AC;
            }
            else
            {
                result.Result = JudgeResult.WA;
                result.Differences = compareResult.Differences;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            result.Result = JudgeResult.TLE;
            return result;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[JudgeService] 运行被测程序失败: {ex}");
            result.Result = JudgeResult.RE;
            return result;
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static bool ValidateOptions(ExecutionOptions options)
    {
        if (string.IsNullOrEmpty(options.ExecutablePath))
            return false;

        return File.Exists(options.ExecutablePath);
    }

    private static Process CreateProcess(ExecutionOptions options)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = options.ExecutablePath,
            Arguments = options.Arguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = string.IsNullOrEmpty(options.WorkingDirectory)
                ? Path.GetDirectoryName(options.ExecutablePath)
                : options.WorkingDirectory,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom
        };

        return new Process { StartInfo = startInfo };
    }

    private static async Task<string> ReadStreamAsync(Stream stream, Encoding encoding)
    {
        try
        {
            using var reader = new StreamReader(stream, encoding);
            return await reader.ReadToEndAsync();
        }
        catch (ObjectDisposedException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[JudgeService] 读取被测程序输出失败: {ex.Message}");
            return string.Empty;
        }
    }

    private static async Task WriteInputAsync(Process process, string input, Encoding encoding)
    {
        if (string.IsNullOrEmpty(input))
        {
            process.StandardInput.Close();
            return;
        }

        await process.StandardInput.WriteAsync(input);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();
    }

    private static async Task<bool> WaitForExitAsync(Process process, int timeoutMs, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);

        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(KillWaitMilliseconds);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[JudgeService] 结束被测程序失败: {ex.Message}");
        }
    }
}
