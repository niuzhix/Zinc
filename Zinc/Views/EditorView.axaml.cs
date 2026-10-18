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

        var registryOptions = new RegistryOptions(ThemeName.LightPlus);
        var textMateInstallation = CodeEditor.InstallTextMate(registryOptions);
        textMateInstallation.SetGrammar(
            registryOptions.GetScopeByLanguageId(
                registryOptions.GetLanguageByExtension(".cpp").Id));

    }

}