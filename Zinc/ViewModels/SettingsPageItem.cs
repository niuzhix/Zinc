using System;

namespace Zinc.ViewModels;

/// <summary>
/// 设置窗口导航栏的一项：标题、图标字形与对应的 ViewModel 类型。
/// </summary>
public sealed class SettingsPageItem
{
    public SettingsPageItem(string title, string glyph, Type viewModelType)
    {
        Title = title;
        Glyph = glyph;
        ViewModelType = viewModelType;
    }

    public string Title { get; }

    public string Glyph { get; }

    public Type ViewModelType { get; }
}
