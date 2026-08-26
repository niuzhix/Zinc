using Avalonia.Controls;
using AvaloniaEdit.TextMate;
using Microsoft.Extensions.DependencyInjection;
using TextMateSharp.Grammars;
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
    }
}