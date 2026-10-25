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

/// <summary>一次评测运行的输入参数。</summary>
public class ExecutionOptions
{
    public string ExecutablePath { get; set; } = string.Empty;

    public string Arguments { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;

    public string StandardInput { get; set; } = string.Empty;

    public string ExpectedOutput { get; set; } = string.Empty;

    public int TimeLimitMs { get; set; } = 1000;

    /// <summary>内存上限（MB）；0 表示不限制。</summary>
    public int MemoryLimitMB { get; set; }

    public string InputEncoding { get; set; } = "UTF-8";

    public string OutputEncoding { get; set; } = "UTF-8";
}

/// <summary>一次评测运行的结果。</summary>
public class ExecutionResult
{
    public JudgeResult Result { get; set; }

    public int ExitCode { get; set; }

    public string StandardOutput { get; set; } = string.Empty;

    public string ErrorOutput { get; set; } = string.Empty;

    public string ExpectedOutput { get; set; } = string.Empty;

    public TimeSpan ExecutionTime { get; set; }

    public long MemoryUsedBytes { get; set; }

    public List<Difference> Differences { get; set; } = new();

    public bool IsPassed => Result == JudgeResult.AC;
}

/// <summary>输出比对中一处不一致的行。</summary>
public class Difference
{
    public int LineNumber { get; set; }

    public string Actual { get; set; } = string.Empty;

    public string Expected { get; set; } = string.Empty;
}

/// <summary>标准输出与期望输出的比对结果。</summary>
public class ComparisonResult
{
    public bool IsMatch { get; set; }

    public List<Difference> Differences { get; set; } = new();

    public string NormalizedActual { get; set; } = string.Empty;

    public string NormalizedExpected { get; set; } = string.Empty;
}

/// <summary>被测进程的资源占用采样。</summary>
public class ResourceUsage
{
    public TimeSpan CpuTime { get; set; }

    public long PeakMemoryBytes { get; set; }

    public long WorkingSetBytes { get; set; }

    public bool MemoryLimitExceeded { get; set; }
}
