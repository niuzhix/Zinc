using Avalonia.Input;
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

    [ObservableProperty]
    private TextDocument content;

    [ObservableProperty]
    private string? filename = string.Empty;

    [ObservableProperty]
    private string? compileLog = string.Empty;

    public ObservableCollection<TestCase> TestCases { get; } = new();

    [ObservableProperty]
    private TestCase? selectedTestCase;

    [ObservableProperty]
    private TextEditorOptions editorOptions = new()
    {
        ShowTabs = true,
        ShowSpaces = true,
        ShowEndOfLine = true,
        EnableTextDragDrop = true,
        HighlightCurrentLine = false,
        CutCopyWholeLine = true,
    };

    private string? filepath = string.Empty;
    private readonly IReadOnlyList<FileFilter> _filters =
    [
        new(){ Name = "C++代码文件", Patterns = ["*.cpp", "*.cxx"] }
    ];

    public EditorViewModel(ISettingsService<AppSettings> settingsService, IDialogService dialogService, IFileService fileService, IProgramService programService, IJudgeService judgeService, IFormatService formatService, string? _content = null, string? _path = null)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
        _fileService = fileService;
        _programService = programService;
        _judgeService = judgeService;
        _formatService = formatService;

        Content = new TextDocument();
        if (!string.IsNullOrEmpty(_content))
        {
            Content.Insert(0, _content);
        }
        filepath = _path;
        Filename = _path?.Split("\\").Last();

        editorOptions.HighlightCurrentLine = Settings.HighlightCurrentLine;
        editorOptions.CutCopyWholeLine = Settings.CutCopyWholeLine;
    }

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
        if (SelectedTestCase == null) return;

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
        if (string.IsNullOrEmpty(filepath))
        {
            var selectedpath = await _dialogService.SaveFilePathAsync("选择保存文件位置", "未标题", ".cpp", _filters);
            if (string.IsNullOrEmpty(selectedpath))
            {
                return false;
            }
            filepath = selectedpath;
        }

        _fileService.SaveFile(filepath, Content.Text);
        Filename = filepath?.Split("\\").Last();
        return true;
    }

    [RelayCommand]
    public async Task SaveAsAsync()
    {
        var selectedpath = await _dialogService.SaveFilePathAsync("选择保存文件位置", "未标题", ".cpp", _filters);
        if (string.IsNullOrEmpty(selectedpath))
        {
            return;
        }

        _fileService.SaveFile(selectedpath, Content.Text);
    }

    [RelayCommand]
    private async Task CompileAsync()
    {
        if(!await SaveAsync())
        {
            CompileLog += $"[{DateTime.Now:T}] [编译取消]\n";
            return;
        }
        var compilers = _programService.FindAllCompilers();
        foreach (var compiler in compilers)
        {
            Console.WriteLine($"[{(compiler.IsDefault ? "默认" : "    ")}] {compiler.Path}");
            Console.WriteLine($"    版本: {compiler.Version}");
        }

        var options = new CompileOptions
        {
            CodePath = filepath,
            CompilerPath = Settings.CompilerPath,
            enableO2 = Settings.EnableO2,
            enableGDB = Settings.EnableGDB,
            StandardVersion = Settings.StandardVersion,
            warningCheck = Settings.WarningCheck,
            overAddressCheck = Settings.OverAddressCheck
        };

        CompileLog += $"[{DateTime.Now:T}] [开始编译] {Filename}\n";

        CompileResult result = await _programService.CompileAsync(options);

        CompileLog += _programService.LogCompileResult(result);
    }

    [RelayCommand]
    private async Task JudgeAsync()
    {
        if (string.IsNullOrEmpty(filepath))
        {
            return;
        }

        if (TestCases.Count == 0)
        {
            CompileLog += $"[{DateTime.Now:T}] [无测试点]\n";
            return;
        }

        string executablePath = OperatingSystem.IsWindows()
            ? Path.ChangeExtension(filepath, ".exe")
            : Path.ChangeExtension(filepath, null);

        int passed = 0;

        foreach (var tc in TestCases)
        {
            if (string.IsNullOrEmpty(tc.Input) || string.IsNullOrEmpty(tc.ExpectedOutput))
            {
                tc.Result = null;
                continue;
            }

            var options = new ExecutionOptions
            {
                ExecutablePath = executablePath,
                StandardInput = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                TimeLimitMs = 2000,
                MemoryLimitMB = 256
            };

            var result = await _judgeService.ExecuteAsync(options);

            tc.Result = result.Result;
            tc.ActualOutput = result.StandardOutput;
            tc.ErrorOutput = result.ErrorOutput;
            tc.ExecutionTimeMs = result.ExecutionTime.TotalMilliseconds;

            if (result.Result == JudgeResult.AC)
                passed++;

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

        CompileLog += $"[{DateTime.Now:T}] [测试完成] 通过 {passed}/{TestCases.Count}\n";
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
        if (!result.Success) {CompileLog += result.Error; return; }

        using (Content.RunUpdate())
            Content.Text = result.Text;
    }

    //[RelayCommand]
    //private async Task FormatSelectionAsync()
    //{
    //    int start = _editor.SelectionStart;
    //    int length = _editor.SelectionLength;

    //    if (length == 0) { await FormatDocumentAsync(); return; }

    //    var selected = Content.GetText(start, length);
    //    var result = await _format.FormatAsync(selected);
    //    if (!result.Success) return;

    //    Content.Replace(start, length, result.Text);
    //}

    public AppSettings Settings => _settingsService.Current;
    public void SaveSettings() => _settingsService.Save();
}