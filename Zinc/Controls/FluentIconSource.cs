using Avalonia.Media;
using FluentAvalonia.UI.Controls;

namespace Zinc.Controls;

/// <summary>
/// 带 Fluent 系统图标字体的图标源，供 XAML 以 <c>{controls:FluentIconSource 字形}</c> 使用。
/// 该类型被 XAML 编译器按标记扩展约定解析，必须提供 <see cref="ProvideValue"/>。
/// </summary>
public class FluentIconSource : FAFontIconSource
{
    private const string IconFontUri = "avares://Zinc/Assets/Fonts/#FluentSystemIcons-Resizable";

    public FluentIconSource(string glyph)
    {
        Glyph = glyph;
        FontFamily = new FontFamily(IconFontUri);
    }

    // XAML 标记扩展约定方法，删除后所有 {controls:FluentIconSource …} 用法都会编译失败。
    public FluentIconSource ProvideValue() => this;
}
