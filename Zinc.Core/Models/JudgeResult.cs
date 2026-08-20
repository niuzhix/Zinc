namespace Zinc.Core.Models;

public enum JudgeResult
{
    AC,
    WA,
    TLE,
    MLE,
    RE,
    CE,
    UKE
}

public class ExecutionOptions
{
    public string ExecutablePath { get; set; }
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string StandardInput { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public int TimeLimitMs { get; set; } = 1000;
    public int MemoryLimitMB { get; set; } = 0;
    public string InputEncoding { get; set; } = "UTF-8";
    public string OutputEncoding { get; set; } = "UTF-8";
}

public class ExecutionResult
{
    public JudgeResult Result { get; set; }
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; }
    public string ErrorOutput { get; set; }
    public string ExpectedOutput { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public long MemoryUsedBytes { get; set; }
    public List<Difference> Differences { get; set; } = new List<Difference>();
    public bool IsPassed => Result == JudgeResult.AC;
}

public class Difference
{
    public int LineNumber { get; set; }
    public string Actual { get; set; }
    public string Expected { get; set; }
}

public class ComparisonResult
{
    public bool IsMatch { get; set; }
    public List<Difference> Differences { get; set; } = new List<Difference>();
    public string NormalizedActual { get; set; }
    public string NormalizedExpected { get; set; }
}

public class ResourceUsage
{
    public TimeSpan CpuTime { get; set; }
    public long PeakMemoryBytes { get; set; }
    public long WorkingSetBytes { get; set; }
    public bool MemoryLimitExceeded { get; set; }
}