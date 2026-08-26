using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace Zinc.Controls;

public partial class IconText : UserControl
{
    public static readonly StyledProperty<string?> SymbolProperty = AvaloniaProperty.Register<IconText, string?>(
        nameof(Glyph));

    public string? Glyph
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<IconText, string>(
        nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<IconText, double>(
        nameof(Spacing), 4);

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }


    public IconText()
    {
        InitializeComponent();
    }
}