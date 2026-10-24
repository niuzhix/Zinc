using Zinc.Core.Abstractions;
using Zinc.Core.Models;

namespace Zinc.Core.Services;

public class OutputComparer : IOutputComparer
{
    private const string EndOfFileMarker = "<EOF>";

    public ComparisonResult Compare(string actual, string expected)
    {
        var result = new ComparisonResult();

        var normalizedActual = NormalizeOutput(actual ?? string.Empty);
        var normalizedExpected = NormalizeOutput(expected ?? string.Empty);

        result.NormalizedActual = normalizedActual;
        result.NormalizedExpected = normalizedExpected;

        if (string.Equals(normalizedActual, normalizedExpected))
        {
            result.IsMatch = true;
            return result;
        }

        if (string.Equals(normalizedActual.TrimEnd(), normalizedExpected.TrimEnd()))
        {
            result.IsMatch = true;
            return result;
        }

        result.IsMatch = false;
        var actualLines = normalizedActual.Split(new[] { '\n' }, StringSplitOptions.None);
        var expectedLines = normalizedExpected.Split(new[] { '\n' }, StringSplitOptions.None);

        int maxLines = Math.Max(actualLines.Length, expectedLines.Length);
        for (int i = 0; i < maxLines; i++)
        {
            string actualLine = i < actualLines.Length ? actualLines[i] : EndOfFileMarker;
            string expectedLine = i < expectedLines.Length ? expectedLines[i] : EndOfFileMarker;

            if (actualLine != expectedLine)
            {
                result.Differences.Add(new Difference
                {
                    LineNumber = i + 1,
                    Actual = actualLine,
                    Expected = expectedLine,
                });
            }
        }

        if (result.Differences.Count == 0)
        {
            result.Differences.Add(new Difference
            {
                LineNumber = 0,
                Actual = normalizedActual,
                Expected = normalizedExpected,
            });
        }

        return result;
    }

    private static string NormalizeOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
