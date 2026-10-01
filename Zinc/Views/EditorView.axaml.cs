using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.TextMate;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using TextMateSharp.Grammars;
using Zinc.Models;
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
    }
}