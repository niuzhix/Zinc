using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Zinc.Abstractions;
using Zinc.Core.Models;
using Zinc.Models;

namespace Zinc.ViewModels;

public partial class SPUIViewModel : ObservableObject
{
    private readonly ISettingsService<AppSettings> _settingsService;

    private static readonly FontFamily FallbackFont =
        new("avares://Zinc/Assets/Fonts#FiraCode Nerd Font Propo Med");

    private static readonly FontFamily SystemFallback =
        new("Consolas");

    public SPUIViewModel(ISettingsService<AppSettings> settingsService)
    {
        _settingsService = settingsService;

        FontFamilies.Add(FallbackFont);

        foreach (var f in FontManager.Current.SystemFonts.OrderBy(x => x.Name))
            FontFamilies.Add(f);

        Settings.EditorFont = Resolve(Settings.EditorFont, Settings.EditorFontWeight);
    }

    [ObservableProperty]
    private string testText =
        "#include <bits/stdc++.h>\nusing namespace std;\nint main(){\n    //处理逻辑\n    return 0;\n}";

    public AppSettings Settings => _settingsService.Current;

    public ObservableCollection<FontFamily> FontFamilies { get; } = new();

    public IReadOnlyList<FontWeight> AvailableFontWeights { get; } = new[]
    {
        FontWeight.Thin,
        FontWeight.ExtraLight,
        FontWeight.Light,
        FontWeight.Normal,
        FontWeight.Medium,
        FontWeight.DemiBold,
        FontWeight.Bold,
        FontWeight.ExtraBold,
        FontWeight.Black,
        FontWeight.ExtraBlack,
    };

    public FontFamily SelectedFont
    {
        get => Settings.EditorFont;
        set
        {
            if (value is null) return;
            Settings.EditorFont = Resolve(value, Settings.EditorFontWeight);
        }
    }

    public FontWeight SelectedFontWeight
    {
        get => Settings.EditorFontWeight;
        set
        {
            Settings.EditorFontWeight = value;
            Settings.EditorFont = Resolve(Settings.EditorFont, value);
        }
    }

    private static FontFamily Resolve(FontFamily preferred, FontWeight weight)
    {
        if (IsUsable(preferred, weight)) return preferred;

        if (IsUsable(FallbackFont, weight)) return FallbackFont;

        if (IsUsable(FallbackFont, FontWeight.Normal)) return FallbackFont;

        return SystemFallback;
    }

    private static bool IsUsable(FontFamily family, FontWeight weight)
    {
        try
        {
            var typeface = new Typeface(family, FontStyle.Normal, weight);
            return FontManager.Current.TryGetGlyphTypeface(typeface, out _);
        }
        catch
        {
            return false;
        }
    }
}