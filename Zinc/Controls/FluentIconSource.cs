using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace Zinc.Controls;

public class FluentIconSource : FAFontIconSource
{
    public FluentIconSource(string glyph)
    {
        Glyph = glyph;
        FontFamily = new FontFamily("avares://Zinc/Assets/Fonts/#FluentSystemIcons-Resizable");
    }

    public FluentIconSource ProvideValue() => this;
}