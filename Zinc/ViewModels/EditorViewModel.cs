using AvaloniaEdit;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zinc.Abstractions;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Models;

namespace Zinc.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly ISettingsService<AppSettings> _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IFileService _fileService;
    private readonly IProgramService _programService;
    private readonly IJudgeService _judgeService;
    private readonly IFormatService _formatService;

    private readonly IReadOnlyList<FileFilter> _filters =
    [
        new() { Name = "C++代码文件", Patterns = ["*.cpp", "*.cxx"] }
    ];

    private const int DefaultTimeLimitMs = 2000;
    private const int DefaultMemoryLimitMB = 256;

    // ====== 可绑定状态 ======

    [ObservableProperty]
    private TextDocument _content;

    [ObservableProperty]
    private string? _fileName = string.Empty;

    [ObservableProperty]
    private string? _filePath = string.Empty;

    [ObservableProperty]
    private string? _log = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private TestCase? _selectedTestCase;

    [ObservableProperty]
    private TextEditorOptions _editorOptions = new()
    {
        ShowTabs = true,
        ShowSpaces = true,
        ShowEndOfLine = true,
        EnableTextDragDrop = true,
        HighlightCurrentLine = false,
        CutCopyWholeLine = true,
    };

    public ObservableCollection<TestCase> TestCases { get; } = new();

    public AppSettings Settings => _settingsService.Current;

    /// <summary>FATabView 显示用标题</summary>
    public string Header => IsDirty
        ? $"{DisplayName} *"
        : DisplayName;

    private string DisplayName =>
        string.IsNullOrEmpty(FileName) ? "未标题" : FileName;

    // ====== 构造 ======

    public EditorViewModel(
        ISettingsService<AppSettings> settingsService,
        IDialogService dialogService,
        IFileService fileService,
        IProgramService programService,
        IJudgeService judgeService,
        IFormatService formatService,
        string? content = null,
        string? path = null)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
        _fileService = fileService;
        _programService = programService;
        _judgeService = judgeService;
        _formatService = formatService;

        _content = new TextDocument();
        if (!string.IsNullOrEmpty(content))
        {
            _content.Insert(0, content);
        }
        _content.TextChanged += (_, _) => IsDirty = true;

        FilePath = path;
        FileName = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);

        EditorOptions.HighlightCurrentLine = Settings.HighlightCurrentLine;
        EditorOptions.CutCopyWholeLine = Settings.CutCopyWholeLine;
    }

    partial void OnFileNameChanged(string? value)
        => OnPropertyChanged(nameof(Header));

    partial void OnIsDirtyChanged(bool value)
        => OnPropertyChanged(nameof(Header));


    [RelayCommand]
    private void AddTestCase()
    {
        var tc = new TestCase { Index = TestCases.Count + 1 };
        TestCases.Add(tc);
        SelectedTestCase = tc;
    }

    [RelayCommand]
    private void RemoveTestCase()
    {
        if (SelectedTestCase is null) return;

        TestCases.Remove(SelectedTestCase);
        for (int i = 0; i < TestCases.Count; i++)
        {
            TestCases[i].Index = i + 1;
        }
        SelectedTestCase = TestCases.LastOrDefault();
    }

    [RelayCommand]
    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            var selectedPath = await _dialogService.SaveFilePathAsync(
                "选择保存文件位置", "未标题", ".cpp", _filters);

            if (string.IsNullOrEmpty(selectedPath))
                return false;

            FilePath = selectedPath;
            FileName = Path.GetFileName(selectedPath);
        }

        _fileService.SaveFile(FilePath, Content.Text);
        IsDirty = false;
        return true;
    }

    [RelayCommand]
    public async Task SaveAsAsync()
    {
        var selectedPath = await _dialogService.SaveFilePathAsync(
            "选择保存文件位置", FileName ?? "未标题", ".cpp", _filters);

        if (string.IsNullOrEmpty(selectedPath))
            return;

        FilePath = selectedPath;
        FileName = Path.GetFileName(selectedPath);

        _fileService.SaveFile(selectedPath, Content.Text);
        IsDirty = false;
    }

    [RelayCommand]
    private async Task CompileAsync()
    {
        if (!await SaveAsync())
        {
            AppendLog("编译取消");
            return;
        }

        foreach (var compiler in _programService.FindAllCompilers())
        {
            var tag = compiler.IsDefault ? "默认" : "    ";
            Console.WriteLine($"[{tag}] {compiler.Path}");
            Console.WriteLine($"    版本: {compiler.Version}");
        }

        var options = new CompileOptions
        {
            CodePath = FilePath,
            CompilerPath = Settings.CompilerPath,
            enableO2 = Settings.EnableO2,
            enableGDB = Settings.EnableGDB,
            StandardVersion = Settings.StandardVersion,
            warningCheck = Settings.WarningCheck,
            overAddressCheck = Settings.OverAddressCheck
        };

        AppendLog($"开始编译 {FileName}");

        var result = await _programService.CompileAsync(options);

        Log += _programService.LogCompileResult(result);
    }

    [RelayCommand]
    private async Task JudgeAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
            return;

        if (TestCases.Count == 0)
        {
            AppendLog("无测试点");
            return;
        }

        var executablePath = OperatingSystem.IsWindows()
            ? Path.ChangeExtension(FilePath, ".exe")
            : Path.ChangeExtension(FilePath, null);

        int passed = 0;

        foreach (var tc in TestCases)
        {
            tc.Result = null;
            if (string.IsNullOrEmpty(tc.Input) || string.IsNullOrEmpty(tc.ExpectedOutput))
            {
                continue;
            }

            var options = new ExecutionOptions
            {
                ExecutablePath = executablePath,
                StandardInput = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                TimeLimitMs = DefaultTimeLimitMs,
                MemoryLimitMB = DefaultMemoryLimitMB
            };

            var result = await _judgeService.ExecuteAsync(options);

            tc.Result = result.Result;
            tc.ActualOutput = result.StandardOutput;
            tc.ErrorOutput = result.ErrorOutput;
            tc.ExecutionTimeMs = result.ExecutionTime.TotalMilliseconds;

            if (result.Result == JudgeResult.AC)
                passed++;

            LogTestResult(tc, result);
        }

        AppendLog($"测试完成 通过 {passed}/{TestCases.Count}");
    }

    [RelayCommand]
    private async Task CompileAndJudgeAsync()
    {
        await CompileAsync();
        await JudgeAsync();
    }

    [RelayCommand]
    private async Task FormatDocumentAsync()
    {
        var result = await _formatService.FormatAsync(Content.Text);
        if (!result.Success)
        {
            Log += result.Error;
            return;
        }

        using (Content.RunUpdate())
        {
            Content.Text = result.Text;
        }

        IsDirty = true;
    }

    private void AppendLog(string message)
        => Log += $"[{DateTime.Now:T}] [{message}]\n";

    private void LogTestResult(TestCase tc, ExecutionResult result)
    {
        Console.WriteLine($"[测试点 {tc.Index}] 状态: {result.Result}, " +
                          $"耗时: {result.ExecutionTime.TotalMilliseconds:F2}ms");

        if (result.Result == JudgeResult.WA && result.Differences.Count > 0)
        {
            foreach (var diff in result.Differences)
            {
                Console.WriteLine($"  {diff.Actual}");
            }
        }

        if (!string.IsNullOrEmpty(result.ErrorOutput))
        {
            Console.WriteLine($"=== 错误输出 ===\n{result.ErrorOutput}");
        }
    }
}