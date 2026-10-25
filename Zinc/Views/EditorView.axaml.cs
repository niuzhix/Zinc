using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace Zinc.Views;

public partial class EditorView : UserControl
{
    private const string CppExtension = ".cpp";

    public EditorView()
    {
        InitializeComponent();

        var registryOptions = new RegistryOptions(IsDarkTheme ? ThemeName.DarkPlus : ThemeName.LightPlus);
        var installation = CodeEditor.InstallTextMate(registryOptions);
        installation.SetGrammar(
            registryOptions.GetScopeByLanguageId(
                registryOptions.GetLanguageByExtension(CppExtension).Id));

        // 主题切换后重新安装，使语法配色跟随明暗主题。
        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += (_, _) =>
            {
                var options = new RegistryOptions(IsDarkTheme ? ThemeName.DarkPlus : ThemeName.LightPlus);
                CodeEditor.InstallTextMate(options).SetGrammar(
                    options.GetScopeByLanguageId(options.GetLanguageByExtension(CppExtension).Id));
            };
        }
    }

    private static bool IsDarkTheme
        => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
}
