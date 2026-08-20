namespace Zinc.Core.Abstractions;

using Zinc.Core.Models;
using System.Diagnostics;

public interface IJudgeService
{
    ExecutionResult Execute(ExecutionOptions options);
    Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, CancellationToken cancellationToken = default);
}

public interface IOutputComparer
{
    ComparisonResult Compare(string actual, string expected);
}

public interface IProcessMonitor
{
    Task<ResourceUsage> MonitorAsync(Process process, long memoryLimitBytes = 0, CancellationToken cancellationToken = default);
}