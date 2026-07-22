using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class EditorView : UserControl
{
    public EditorView()
    {
        InitializeComponent();
        DataContext = new EditorViewModel();

        var  _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var _textMateInstallation = CodeEditor.InstallTextMate(_registryOptions);
        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".cpp").Id));
    }
}