using CommunityToolkit.Mvvm.ComponentModel;
using Zinc.Core.Models;
using Zinc.Models;

namespace Zinc.Models;

public partial class TestCase : ObservableObject
{
    public int Index { get; set; }

    [ObservableProperty]
    private string? input;

    [ObservableProperty]
    private string? expectedOutput;

    [ObservableProperty]
    private string? actualOutput;

    [ObservableProperty]
    private string? errorOutput;

    [ObservableProperty]
    private JudgeResult? result;

    [ObservableProperty]
    private double executionTimeMs;
}