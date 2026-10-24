using Zinc.Core.Models;

namespace Zinc.Core.Abstractions;

public interface IProgramService
{
    Task<CompileResult> CompileAsync(CompileOptions options);

    /// <summary>发现可用编译器；同一进程内只探测一次。</summary>
    Task<IReadOnlyList<CompilerInfo>> FindAllCompilersAsync();

    string LogCompileResult(CompileResult result);
}
