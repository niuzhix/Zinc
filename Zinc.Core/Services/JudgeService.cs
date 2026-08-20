using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using System;
using System.Diagnostics;
using System.Text;

public class JudgeService : IJudgeService
{
    private readonly IOutputComparer _outputComparer;
    private readonly IProcessMonitor _processMonitor;

    public JudgeService(IOutputComparer outputComparer = null, IProcessMonitor processMonitor = null)
    {
        _outputComparer = outputComparer ?? new OutputComparer();
        _processMonitor = processMonitor ?? new ProcessMonitor();
    }

    public ExecutionResult Execute(ExecutionOptions options)
    {
        return ExecuteAsync(options).GetAwaiter().GetResult();
    }

    public async Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, CancellationToken cancellationToken = default)
    {
        if (!ValidateOptions(options))
            return new ExecutionResult
            {
                Result = JudgeResult.CE
            };
        

        var result = new ExecutionResult
        {
            ExpectedOutput = options.ExpectedOutput,
            Result = JudgeResult.UKE
        };

        Process process = null;
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
                cancellationToken
            );

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
                result.Result = JudgeResult.RE;
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
            result.Result = JudgeResult.RE;
            return result;
        }
        finally
        {
            process?.Dispose();
        }
    }

    private bool ValidateOptions(ExecutionOptions options)
    {
        if (string.IsNullOrEmpty(options.ExecutablePath))
            return false;

        if (!File.Exists(options.ExecutablePath))
            return false;
        return true;
    }

    private Process CreateProcess(ExecutionOptions options)
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
                : options.WorkingDirectory
        };

        return new Process { StartInfo = startInfo };
    }

    private async Task<string> ReadStreamAsync(Stream stream, Encoding encoding)
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
            return ex.Message;
        }
    }

    private async Task WriteInputAsync(Process process, string input, Encoding encoding)
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

    private async Task<bool> WaitForExitAsync(Process process, int timeoutMs, CancellationToken cancellationToken)
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

    private void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
                process.WaitForExit(1000);
            }
        }
        catch { }
    }
}

public class OutputComparer : IOutputComparer
{
    public ComparisonResult Compare(string actual, string expected)
    {
        var result = new ComparisonResult();

        var normalizedActual = NormalizeOutput(actual ?? string.Empty);
        var normalizedExpected = NormalizeOutput(expected ?? string.Empty);

        result.NormalizedActual = normalizedActual;
        result.NormalizedExpected = normalizedExpected;

        if (string.Equals(normalizedActual, normalizedExpected))
        {
            result.IsMatch = true;
            return result;
        }

        if (string.Equals(normalizedActual.TrimEnd(), normalizedExpected.TrimEnd()))
        {
            result.IsMatch = true;
            return result;
        }

        result.IsMatch = false;
        var actualLines = normalizedActual.Split(new[] { '\n' }, StringSplitOptions.None);
        var expectedLines = normalizedExpected.Split(new[] { '\n' }, StringSplitOptions.None);

        int maxLines = Math.Max(actualLines.Length, expectedLines.Length);
        for (int i = 0; i < maxLines; i++)
        {
            string actualLine = i < actualLines.Length ? actualLines[i] : "<EOF>";
            string expectedLine = i < expectedLines.Length ? expectedLines[i] : "<EOF>";

            if (actualLine != expectedLine)
            {
                result.Differences.Add(new Difference
                {
                    LineNumber = i + 1,
                    Actual = actualLine,
                    Expected = expectedLine,
                });
            }
        }

        if (result.Differences.Count == 0)
        {
            result.Differences.Add(new Difference
            {
                LineNumber = 0,
                Actual = normalizedActual,
                Expected = normalizedExpected,
            });
        }

        return result;
    }

    private string NormalizeOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}

public class ProcessMonitor : IProcessMonitor
{
    public async Task<ResourceUsage> MonitorAsync(Process process, long memoryLimitBytes = 0, CancellationToken cancellationToken = default)
    {
        var usage = new ResourceUsage();

        try
        {
            while (!process.HasExited && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    process.Refresh();

                    usage.CpuTime = process.TotalProcessorTime;

                    var memoryBytes = process.PeakWorkingSet64;
                    if (memoryBytes > usage.PeakMemoryBytes)
                    {
                        usage.PeakMemoryBytes = memoryBytes;
                    }

                    usage.WorkingSetBytes = process.WorkingSet64;

                    if (memoryLimitBytes > 0 && process.WorkingSet64 > memoryLimitBytes)
                    {
                        usage.MemoryLimitExceeded = true;
                        break;
                    }
                }
                catch (InvalidOperationException)
                {
                    break;
                }

                await Task.Delay(50, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }

        return usage;
    }
}