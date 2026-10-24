using System.Diagnostics;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;

namespace Zinc.Core.Services;

public class ProcessMonitor : IProcessMonitor
{
    private const int PollIntervalMilliseconds = 50;

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

                await Task.Delay(PollIntervalMilliseconds, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 等待被取消属于正常结束路径。
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[ProcessMonitor] 采样进程资源失败: {ex.Message}");
        }

        return usage;
    }
}
