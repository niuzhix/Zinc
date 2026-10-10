namespace Zinc.Core.Abstractions;

public interface IFormatService
{
    Task<FormatResult> FormatAsync(string sourceCode, string style = "file", int offset = -1);
}

public sealed record FormatResult(bool Success, string Text, int CursorOffset = 0, string? Error = null)
{
    public static FormatResult Ok(string text, int cursorOffset) => new(true, text, cursorOffset);
    public static FormatResult Fail(string error) => new(false, string.Empty, 0, error);
}