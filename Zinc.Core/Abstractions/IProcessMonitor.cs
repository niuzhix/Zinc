using System.Diagnostics;
using Zinc.Core.Models;

namespace Zinc.Core.Abstractions;

public interface IProcessMonitor
{
    Task<ResourceUsage> MonitorAsync(Process process, long memoryLimitBytes = 0, CancellationToken cancellationToken = default);
}
