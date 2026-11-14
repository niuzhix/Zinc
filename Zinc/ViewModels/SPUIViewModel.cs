using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Zinc.Abstractions;
using Zinc.Models;
using Zinc.Services;

namespace Zinc.ViewModels;

public partial class SPUIViewModel : ObservableObject
{
    private readonly ISettingsService<AppSettings> _settingsService;

    public SPUIViewModel(ISettingsService<AppSettings> settingsService)
    {
        _settingsService = settingsService;

        foreach (var f in EditorFontResolver.SystemFonts)
        {
            FontFamilies.Add(f);
        }

        EnsureFontInList(SelectedFont);

        _selectedTabPosition = TabPositionOptions.FirstOrDefault(o => o.Key == Settings.TabPositon);
    }

    [ObservableProperty]
    private string _testText =
        "#include <bits/stdc++.h>\nusing namespace std;\nint main(){\n    //处理逻辑\n    return 0;\n}";

    public AppSettings Settings => _settingsService.Current;

    public ObservableCollection<FontFamily> FontFamilies { get; } = new();

    public IReadOnlyList<FontWeight> AvailableFontWeights => EditorFontResolver.AvailableWeights;

    public IReadOnlyList<KeyValuePair<FATabViewTabStripLocation, string>> TabPositionOptions { get; } =
    [
        new(FATabViewTabStripLocation.Top, "顶部"),
        new(FATabViewTabStripLocation.Bottom, "底部"),
        new(FATabViewTabStripLocation.Left, "左侧"),
        new(FATabViewTabStripLocation.Right, "右侧"),
    ];

    private KeyValuePair<FATabViewTabStripLocation, string> _selectedTabPosition;
    public KeyValuePair<FATabViewTabStripLocation, string> SelectedTabPosition
    {
        get => _selectedTabPosition;
        set
        {
            if (SetProperty(ref _selectedTabPosition, value))
                TabPositon = value.Key;
        }
    }
    public FATabViewTabStripLocation TabPositon
    {
        get => Settings.TabPositon;
        set
        {
            if (Settings.TabPositon == value) return;

            Settings.TabPositon = value;

            OnPropertyChanged(nameof(TabPositon));

            var newSelected = TabPositionOptions.FirstOrDefault(o => o.Key == value);
            if (_selectedTabPosition.Key != newSelected.Key || _selectedTabPosition.Value != newSelected.Value)
            {
                _selectedTabPosition = newSelected;
                OnPropertyChanged(nameof(SelectedTabPosition));
            }
        }
    }

    /// <summary>当前生效的字体：显示用已解析结果，不写回设置；只有用户选择时才持久化。</summary>
    public FontFamily SelectedFont
    {
        get => EditorFontResolver.Resolve(Settings.EditorFont, Settings.EditorFontWeight);
        set
        {
            if (value is null) return;

            var resolved = EditorFontResolver.Resolve(value, Settings.EditorFontWeight);
            if (Equals(resolved, Settings.EditorFont)) return;

            Settings.EditorFont = resolved;
            OnPropertyChanged();
            EnsureFontInList(resolved);
        }
    }

    public FontWeight SelectedFontWeight
    {
        get => Settings.EditorFontWeight;
        set
        {
            var normalized = EditorFontResolver.NormalizeWeight(value);
            if (normalized == Settings.EditorFontWeight) return;

            Settings.EditorFontWeight = normalized;
            OnPropertyChanged();

            var resolved = EditorFontResolver.Resolve(Settings.EditorFont, normalized);
            if (!Equals(resolved, Settings.EditorFont))
            {
                Settings.EditorFont = resolved;
                OnPropertyChanged(nameof(SelectedFont));
                EnsureFontInList(resolved);
            }
        }
    }

    private void EnsureFontInList(FontFamily font)
    {
        if (FontFamilies.Any(f => Equals(f, font))) return;
        FontFamilies.Insert(0, font);
    }
}
