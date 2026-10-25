using CommunityToolkit.Mvvm.ComponentModel;
using Zinc.Core.Models;

namespace Zinc.Models;

/// <summary>一个测试点：输入、期望输出与最近一次评测的观测值。</summary>
public partial class TestCase : ObservableObject
{
    /// <summary>从 1 开始的序号，删除测试点后由 <c>EditorViewModel</c> 重排。</summary>
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string? _input;

    [ObservableProperty]
    private string? _expectedOutput;

    [ObservableProperty]
    private string? _actualOutput;

    [ObservableProperty]
    private string? _errorOutput;

    [ObservableProperty]
    private JudgeResult? _result;

    [ObservableProperty]
    private double _executionTimeMs;
}
