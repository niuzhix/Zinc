using Zinc.Core.Models;

namespace Zinc.Core.Abstractions;

public interface IProgramService
{
    Task<int> CompileAsync(CompileOptions options);
}
