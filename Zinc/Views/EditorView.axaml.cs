using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.TextMate;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using TextMateSharp.Grammars;
using Zinc.Models;
using Zinc.Services;
using Zinc.Abstractions;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class EditorView : UserControl
{
    public EditorView(string? content = null, string? path = null)
    {
        InitializeComponent();
        DataContext = ActivatorUtilities.CreateInstance<EditorViewModel>(
            App.Services,
            content ?? "",
            path ?? ""
        );

        var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var _textMateInstallation = CodeEditor.InstallTextMate(_registryOptions);
        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".cpp").Id));

        CodeEditor.TextArea.TextEntering += OnTextEntering;
    }

    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text)) return;
        var textArea = CodeEditor.TextArea;
        int offset = textArea.Caret.Offset;

        var pairs = new Dictionary<char, char>
        {
            { '(', ')' }, { '[', ']' }, { '{', '}' }, { '"', '"' }, { '\'', '\'' }
        };
        var pairs_out = new List<char>
        {
            ')', ']', '}', '"', '\''
        };

        // 括号匹配
        char input = e.Text[0];
        if (pairs.TryGetValue(input, out char closing))
        {
            if (offset < textArea.Document.TextLength && textArea.Document.GetCharAt(offset) == closing)
            {
                textArea.Caret.Offset = offset + 1;
                e.Handled = true;
                return;
            }
            textArea.Document.Insert(offset, closing.ToString());
            textArea.Caret.Offset = offset;
        }
        else if (pairs_out.Contains(input)){
            textArea.Caret.Offset++;
            e.Handled = true;
            return;
        }

        //自动缩进
        if(offset > 0)
        {
            char prev = textArea.Document.GetCharAt(offset - 1);

            if(prev == '{')
            {
                string indent = textArea.Options.IndentationString;
                string newLine = Environment.NewLine;

                e.Handled = true;

                textArea.Document.Insert(offset, newLine + indent + newLine);
                textArea.Caret.Offset = offset + newLine.Length + indent.Length;
            }
        }

        //自动格式化
        if (App.Services.GetService<ISettingsService<AppSettings>>().Current.AutoFormatting)
        {
            if(input == ';')
            {
                if (DataContext is EditorViewModel vm)
                {
                    textArea.Document.Insert(offset, ";");
                    vm.FormatDocumentCommand.Execute(null);
                }
            }
        }
    }
}