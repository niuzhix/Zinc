using Zinc.Core.Models;

namespace Zinc.Core.Abstractions;

public interface IProgramService
{
    Task<CompileResult> CompileAsync(CompileOptions options);

    List<CompilerInfo> FindAllCompilers();

    string LogCompileResult(CompileResult result);
}
