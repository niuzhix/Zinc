namespace Zinc.Core.Models;

public class CompileResult
{
    public bool IsSuccess { get; set; }

    public int ExitCode { get; set; }

    public string Output { get; set; } = string.Empty;

    public string Error { get; set; } = string.Empty;

    public string CompilerPath { get; set; } = string.Empty;

    public string FullCommand { get; set; } = string.Empty;

    public long ElapsedMilliseconds { get; set; }

    public CompileErrorType ErrorType { get; set; } = CompileErrorType.None;

    public string ErrorMessage { get; set; } = string.Empty;
}

public class CompilerInfo
{
    public string Path { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

public enum CompileErrorType
{
    None = 0,
    CompilerNotFound = -1,
    SourceFileNotFound = -2,
    InternalError = -3,
    CompilationFailed = 1
}