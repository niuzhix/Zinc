using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using System;
using System.Collections.Generic;
using TextMateSharp.Grammars;
using Zinc.Core.Models;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class EditorView : UserControl
{
    public EditorView()
    {
        InitializeComponent();

        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var textMateInstallation = CodeEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(
            registryOptions.GetScopeByLanguageId(
                registryOptions.GetLanguageByExtension(".cpp").Id));

        CodeEditor.TextArea.TextEntering += OnTextEntering;
    }

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text)) return;

        var textArea = CodeEditor.TextArea;
        int offset = textArea.Caret.Offset;
        char input = e.Text[0];

        var pairs = new Dictionary<char, char>
        {
            { '(', ')' }, { '[', ']' }, { '{', '}' }, { '"', '"' }, { '\'', '\'' }
        };
        var closingChars = new HashSet<char> { ')', ']', '}', '"', '\'' };

        // 括号配对：输入左括号时自动补右括号
        if (pairs.TryGetValue(input, out char closing))
        {
            if (offset < textArea.Document.TextLength
                && textArea.Document.GetCharAt(offset) == closing)
            {
                // 后面已有右括号，跳过
                textArea.Caret.Offset = offset + 1;
                e.Handled = true;
                return;
            }

            textArea.Document.Insert(offset, closing.ToString());
            textArea.Caret.Offset = offset;
            return; // 让输入正常写入
        }

        // 输入右括号：后面已经有相同的就跳过
        if (closingChars.Contains(input)
            && offset < textArea.Document.TextLength
            && textArea.Document.GetCharAt(offset) == input)
        {
            textArea.Caret.Offset = offset + 1;
            e.Handled = true;
            return;
        }

        // 自动缩进：输入 { 后换行
        if (offset > 0 && textArea.Document.GetCharAt(offset - 1) == '{')
        {
            string indent = textArea.Options.IndentationString;
            string newLine = Environment.NewLine;

            e.Handled = true;
            textArea.Document.Insert(offset, newLine + indent + newLine);
            textArea.Caret.Offset = offset + newLine.Length + indent.Length;
            return;
        }

        // 自动格式化：输入 ; 且设置开启
        if (input == ';'
            && DataContext is EditorViewModel vm
            && vm.Settings.AutoFormatting)
        {
            // 让分号正常写入，然后在下一帧触发格式化
            Dispatcher.UIThread.Post(() =>
            {
                if (vm.FormatDocumentCommand.CanExecute(null))
                    vm.FormatDocumentCommand.Execute(null);
            }, DispatcherPriority.Background);
        }
    }
}