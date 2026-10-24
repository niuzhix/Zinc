using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
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
using Zinc.Core.Services;
using Zinc.Models;

namespace Zinc.ViewModels;

public partial class EditorViewModel : ObservableObject, IDisposable
{
    #region Constants

    private const int DefaultTimeLimitMs = 2000;
    private const int DefaultMemoryLimitMB = 256;
    private const string UntitledName = "未标题";
    private const string DefaultExtension = ".cpp";
    private const string SaveDialogTitle = "选择保存文件位置";

    private readonly IReadOnlyList<FileFilter> _filters =
    [
        new() { Name = "C++代码文件", Patterns = ["*.cpp", "*.cxx"] }
    ];

    #endregion

    #region Dependencies and Fields

    private readonly ISettingsService<AppSettings> _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IFileService _fileService;
    private readonly IProgramService _programService;
    private readonly IJudgeService _judgeService;
    private readonly IFormatService _formatService;

    private TextEditor? _editor;
    private bool _autoFormatQueued;

    private static readonly TimeSpan AutoFormatDelay = TimeSpan.FromMilliseconds(400);

    #endregion

    #region Bindable State

    private TextDocument _content = new();

    /// <summary>编辑器的文档；替换时自动转移 TextChanged 订阅。</summary>
    public TextDocument Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(_content, value)) return;

            var oldValue = _content;
            _content = value;
            OnContentChanged(oldValue, value);
            OnPropertyChanged();
        }
    }

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
        ShowEndOfLine = false,
        EnableTextDragDrop = true,
        HighlightCurrentLine = false,
        CutCopyWholeLine = true,
    };

    public ObservableCollection<TestCase> TestCases { get; } = new();

    public AppSettings Settings => _settingsService.Current;

    /// <summary>FATabView 显示用标题</summary>
    public string Header => IsDirty ? $"{DisplayName} *" : DisplayName;

    private string DisplayName =>
        string.IsNullOrEmpty(FileName) ? UntitledName : FileName;

    #endregion

    #region Construction

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

        var document = new TextDocument();
        if (!string.IsNullOrEmpty(content))
            document.Insert(0, content);

        // 先写入内容再交给 Content，订阅在 OnContentChanged 里建立，
        // 初始载入不会被标记为已修改。
        Content = document;

        FilePath = path;
        FileName = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);

        ApplyEditorOptions();
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    private void OnContentChanged(TextDocument? oldValue, TextDocument? newValue)
    {
        if (oldValue is not null)
            oldValue.TextChanged -= OnDocumentTextChanged;

        if (newValue is not null)
            newValue.TextChanged += OnDocumentTextChanged;
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e) => IsDirty = true;

    /// <summary>把设置中影响编辑器行为的项同步到 <see cref="TextEditorOptions"/>。</summary>
    private void ApplyEditorOptions()
    {
        EditorOptions.HighlightCurrentLine = Settings.HighlightCurrentLine;
        EditorOptions.CutCopyWholeLine = Settings.CutCopyWholeLine;
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs<AppSettings> e)
    {
        if (e.PropertyName is null
            or nameof(AppSettings.HighlightCurrentLine)
            or nameof(AppSettings.CutCopyWholeLine))
        {
            ApplyEditorOptions();
        }
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
        Content.TextChanged -= OnDocumentTextChanged;

        if (_editor is not null)
        {
            _editor.TextArea.TextEntering -= OnTextEntering;
            _editor = null;
        }
    }

    partial void OnFileNameChanged(string? value) => OnPropertyChanged(nameof(Header));

    partial void OnIsDirtyChanged(bool value) => OnPropertyChanged(nameof(Header));

    public void AttachEditor(TextEditor editor)
    {
        if (_editor is not null)
        {
            _editor.TextArea.TextEntering -= OnTextEntering;
        }

        _editor = editor;

        if (_editor?.TextArea is null) return;

        if (!ReferenceEquals(_editor.Document, Content))
        {
            _editor.Document = Content;
        }

        _editor.TextArea.TextEntering += OnTextEntering;
    }
    public void DetachEditor(TextEditor editor)
    {
        if (ReferenceEquals(_editor, editor))
        {
            if (_editor?.TextArea is not null)
            {
                _editor.TextArea.TextEntering -= OnTextEntering;
            }
            _editor = null;
        }
    }

    #endregion

    #region Test Case Commands

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
        RenumberTestCases();
        SelectedTestCase = TestCases.LastOrDefault();
    }

    private void RenumberTestCases()
    {
        for (int i = 0; i < TestCases.Count; i++)
            TestCases[i].Index = i + 1;
    }

    #endregion

    #region File Commands

    [RelayCommand]
    public async Task<bool> SaveAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            var selectedPath = await _dialogService.SaveFilePathAsync(
                SaveDialogTitle, UntitledName, DefaultExtension, _filters);

            if (string.IsNullOrEmpty(selectedPath))
                return false;

            ApplyPath(selectedPath);
        }

        WriteFile(FilePath!, Content.Text);
        IsDirty = false;
        return true;
    }

    [RelayCommand]
    public async Task SaveAsAsync()
    {
        var selectedPath = await _dialogService.SaveFilePathAsync(
            SaveDialogTitle, FileName ?? UntitledName, DefaultExtension, _filters);

        if (string.IsNullOrEmpty(selectedPath))
            return;

        ApplyPath(selectedPath);
        WriteFile(selectedPath, Content.Text);
        IsDirty = false;
    }

    private void ApplyPath(string path)
    {
        FilePath = path;
        FileName = Path.GetFileName(path);
    }

    private void WriteFile(string path, string text) => _fileService.SaveFile(path, text);

    #endregion

    #region Compile / Judge Commands

    [RelayCommand]
    private async Task CompileAsync()
    {
        if (!await SaveAsync())
        {
            AppendLog("编译取消");
            return;
        }

        await LogAvailableCompilersAsync();

        AppendLog($"开始编译 {FileName}");
        var result = await _programService.CompileAsync(BuildCompileOptions());
        AppendRawLog(_programService.LogCompileResult(result));
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

        var executablePath = GetExecutablePath(FilePath);
        int passed = 0;

        foreach (var tc in TestCases)
        {
            tc.Result = null;
            if (IsIncomplete(tc)) continue;

            var result = await RunTestCaseAsync(executablePath, tc);
            if (result.Result == JudgeResult.AC) passed++;

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

    private async Task LogAvailableCompilersAsync()
    {
        var compilers = await _programService.FindAllCompilersAsync();
        if (compilers.Count == 0)
        {
            AppendLog("未找到 C++ 编译器");
            return;
        }

        foreach (var compiler in compilers)
        {
            var tag = compiler.IsDefault ? "默认" : "备用";
            AppendLog($"[{tag}] {compiler.Path} — {compiler.Version}");
        }
    }

    private CompileOptions BuildCompileOptions() => new()
    {
        CodePath = FilePath,
        CompilerPath = Settings.CompilerPath,
        enableO2 = Settings.EnableO2,
        enableGDB = Settings.EnableGDB,
        StandardVersion = Settings.StandardVersion,
        warningCheck = Settings.WarningCheck,
        overAddressCheck = Settings.OverAddressCheck
    };

    private static string GetExecutablePath(string codePath) =>
        CompileArtifacts.GetExecutablePath(codePath);

    private static bool IsIncomplete(TestCase tc) =>
        string.IsNullOrEmpty(tc.Input) || string.IsNullOrEmpty(tc.ExpectedOutput);

    private async Task<ExecutionResult> RunTestCaseAsync(string executablePath, TestCase tc)
    {
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

        return result;
    }

    #endregion

    #region Format Commands

    [RelayCommand]
    private async Task FormatDocumentAsync()
    {
        var editor = _editor;
        if (editor?.Document is null) return;

        var oldCaret = editor.CaretOffset;
        var result = await _formatService.FormatAsync(editor.Document.Text, offset: oldCaret);
        if (!result.Success) return;

        // 期间可能已切换编辑器/标签页，或文档已被替换
        if (!ReferenceEquals(_editor, editor)) return;

        ApplyDocumentText(editor, result.Text);
        MoveCaretTo(result.CursorOffset);
    }

    /// <summary>只在文本确实变化时替换文档；等价结果不触发 TextChanged、不置脏。</summary>
    private static void ApplyDocumentText(TextEditor editor, string text)
    {
        if (string.Equals(editor.Document.Text, text, StringComparison.Ordinal))
            return;

        using (editor.Document.RunUpdate())
            editor.Document.Text = text;
    }

    [RelayCommand]
    private async Task FormatSelectionAsync()
    {
        var editor = _editor;
        if (editor?.Document is null) return;

        int start = editor.SelectionStart;
        int length = editor.SelectionLength;

        if (length == 0)
        {
            await FormatDocumentAsync();
            return;
        }

        var selected = editor.Document.GetText(start, length);
        var caretInSelection = editor.CaretOffset - start;

        var result = await _formatService.FormatAsync(selected, offset: caretInSelection);
        if (!result.Success) return;
        if (!ReferenceEquals(_editor, editor)) return;

        if (!string.Equals(selected, result.Text, StringComparison.Ordinal))
        {
            editor.Document.Replace(start, length, result.Text);
        }

        MoveCaretTo(start + result.CursorOffset);
    }

    private void MoveCaretTo(int offset)
    {
        if (_editor == null) return;

        _editor.CaretOffset = offset;
        _editor.ScrollToLine(_editor.Document.GetLineByOffset(offset).LineNumber);
    }

    #endregion

    #region Text Input Handling

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (_editor?.TextArea is not { } textArea) return;
        if (string.IsNullOrEmpty(e.Text)) return;

        var doc = textArea.Document;
        int offset = textArea.Caret.Offset;
        char input = e.Text[0];

        if (TextInputHelper.TryHandleBracket(textArea, doc, offset, input, e)) return;
        if (TextInputHelper.TryAutoIndentOnBrace(textArea, doc, offset, e)) return;

        if (input == ';' && Settings.AutoFormatting)
            RequestAutoFormat();
    }

    /// <summary>把连续输入的分号触发的格式化合并成一次（400ms 窗口内只排一次）。</summary>
    private void RequestAutoFormat()
    {
        if (_autoFormatQueued) return;

        _autoFormatQueued = true;
        DispatcherTimer.RunOnce(
            () =>
            {
                _autoFormatQueued = false;
                TriggerFormatDocument();
            },
            AutoFormatDelay);
    }

    private void TriggerFormatDocument()
    {
        if (FormatDocumentCommand.IsRunning) return;
        if (FormatDocumentCommand.CanExecute(null))
            FormatDocumentCommand.Execute(null);
    }

    #endregion

    #region Logging

    private void AppendLog(string message)
        => AppendRawLog($"[{DateTime.Now:T}] [{message}]");

    /// <summary>追加一段已经格式化好的文本，保证以换行结尾。</summary>
    private void AppendRawLog(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        Log += text.EndsWith('\n') ? text : text + Environment.NewLine;
    }

    private void LogTestResult(TestCase tc, ExecutionResult result)
    {
        AppendLog($"[测试点 {tc.Index}] 状态: {result.Result}, 耗时: {result.ExecutionTime.TotalMilliseconds:F2}ms");

        if (result.Result == JudgeResult.WA && result.Differences.Count > 0)
        {
            foreach (var diff in result.Differences)
                AppendLog($"  第 {diff.LineNumber} 行 实际: {diff.Actual}");
        }

        if (!string.IsNullOrEmpty(result.ErrorOutput))
            AppendRawLog($"=== 错误输出 ===\n{result.ErrorOutput}");
    }

    #endregion
}

internal static class TextInputHelper
{
    private static readonly Dictionary<char, char> BracketPairs = new()
    {
        { '(', ')' }, { '[', ']' }, { '{', '}' }, { '"', '"' }, { '\'', '\'' }
    };

    private static readonly HashSet<char> ClosingChars = new() { ')', ']', '}', '"', '\'' };

    /// <summary>
    /// 处理括号/引号输入。返回 true 表示输入已处理，调用方应直接 return。
    /// </summary>
    public static bool TryHandleBracket(
        TextArea textArea, TextDocument doc, int offset, char input, TextInputEventArgs e)
    {
        // 1. 有选中内容：用括号对包裹
        if (textArea.Selection.Length > 0 && BracketPairs.TryGetValue(input, out char pairClose))
        {
            var segment = textArea.Selection.SurroundingSegment;
            if (segment != null)
            {
                string selected = doc.GetText(segment.Offset, segment.Length);
                doc.Replace(segment.Offset, segment.Length, input + selected + pairClose);
                textArea.Caret.Offset = segment.Offset + segment.Length + 2;
                e.Handled = true;
                return true;
            }
        }

        // 2. 纯闭字符 ) ] }：后面就是它自己 → 跳过
        if (ClosingChars.Contains(input) && !IsQuote(input))
        {
            if (offset < doc.TextLength && doc.GetCharAt(offset) == input)
            {
                textArea.Caret.Offset = offset + 1;
                e.Handled = true;
            }
            return true;
        }

        // 3. 开字符 ( [ { 与引号
        if (BracketPairs.TryGetValue(input, out char closing))
        {
            // 引号后面已是同一个引号 → 跳过
            if (IsQuote(input)
                && offset < doc.TextLength
                && doc.GetCharAt(offset) == input)
            {
                textArea.Caret.Offset = offset + 1;
                e.Handled = true;
                return true;
            }

            // 引号紧跟单词字符 → 不配对，按普通输入处理
            if (IsQuote(input) && offset > 0 && IsWordChar(doc.GetCharAt(offset - 1)))
                return false;

            // 插入配对闭字符，光标留原位
            doc.Insert(offset, closing.ToString());
            textArea.Caret.Offset = offset;
            return true;
        }

        return false;
    }

    /// <summary>
    /// '{' 后换行自动缩进；后面紧跟 '}' 时同时补出收尾缩进。
    /// </summary>
    public static bool TryAutoIndentOnBrace(
        TextArea textArea, TextDocument doc, int offset, TextInputEventArgs e)
    {
        bool isNewLine = e.Text is "\n" or "\r" or "\r\n";
        if (!isNewLine || offset <= 0) return false;
        if (doc.GetCharAt(offset - 1) != '{') return false;

        var line = doc.GetLineByOffset(offset - 1);
        int indentEnd = line.Offset;
        while (indentEnd < line.EndOffset
               && (doc.GetCharAt(indentEnd) == ' ' || doc.GetCharAt(indentEnd) == '\t'))
            indentEnd++;

        string baseIndent = doc.GetText(line.Offset, indentEnd - line.Offset);
        string childIndent = baseIndent + textArea.Options.IndentationString;
        string newline = e.Text;

        // 跳过光标后的空白，检查是否紧跟 '}'
        int probe = offset;
        while (probe < doc.TextLength
               && (doc.GetCharAt(probe) == ' ' || doc.GetCharAt(probe) == '\t'))
            probe++;

        bool hasClose = probe < doc.TextLength && doc.GetCharAt(probe) == '}';

        e.Handled = true;

        if (hasClose)
        {
            doc.Replace(offset, probe - offset,
                newline + childIndent + newline + baseIndent);
        }
        else
        {
            doc.Insert(offset, newline + childIndent);
        }

        textArea.Caret.Offset = offset + newline.Length + childIndent.Length;
        return true;
    }

    private static bool IsQuote(char c) => c is '"' or '\'';

    private static bool IsWordChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_';
}