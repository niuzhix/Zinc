using Zinc.Core.Models;

namespace Zinc.Core.Abstractions;

public interface IJudgeService
{
    Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, CancellationToken cancellationToken = default);
}
