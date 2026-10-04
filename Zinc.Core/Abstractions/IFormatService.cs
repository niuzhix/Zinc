namespace Zinc.Core.Abstractions;

public interface IFormatService
{
    Task<FormatResult> FormatAsync(string sourceCode, string style = "file");
}

public sealed record FormatResult(bool Success, string Text, string? Error = null)
{
    public static FormatResult Ok(string text) => new(true, text);

    public static FormatResult Fail(string error) => new(false, string.Empty, error);
}