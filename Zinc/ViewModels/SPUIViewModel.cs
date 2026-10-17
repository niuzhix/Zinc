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

    private static readonly FontFamily BundledFallback =
        new("avares://Zinc/Assets/Fonts#Fira Code Light");

    private static readonly FontFamily SystemFallback = new("Consolas");

    public SPUIViewModel(ISettingsService<AppSettings> settingsService)
    {
        _settingsService = settingsService;

        foreach (var f in FontManager.Current.SystemFonts
                     .Where(IsConcreteFontFamily)
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            FontFamilies.Add(f);
        }

        var weight = NormalizeWeight(Settings.EditorFontWeight);
        var resolved = Resolve(Settings.EditorFont, weight);

        Settings.EditorFontWeight = weight;
        Settings.EditorFont = resolved;

        EnsureFontInList(resolved);
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

            var resolved = Resolve(value, Settings.EditorFontWeight);
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
            var normalized = NormalizeWeight(value);
            if (normalized == Settings.EditorFontWeight) return;

            Settings.EditorFontWeight = normalized;
            OnPropertyChanged();

            var resolved = Resolve(Settings.EditorFont, normalized);
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

    private FontWeight NormalizeWeight(FontWeight weight)
        => AvailableFontWeights.Contains(weight) ? weight : FontWeight.Normal;

    private static bool IsConcreteFontFamily(FontFamily family)
    {
        if (family is null) return false;

        if (family.Key?.Source == FontManager.Current.SystemFonts.Key) return false;

        if (family.Name?.StartsWith("fonts:", StringComparison.OrdinalIgnoreCase) == true)
            return false;

        return !string.IsNullOrWhiteSpace(family.Name);
    }

    private static FontFamily Resolve(FontFamily? preferred, FontWeight weight)
    {
        if (preferred is not null
            && IsConcreteFontFamily(preferred)
            && IsUsable(preferred, weight))
        {
            return preferred;
        }

        if (IsUsable(BundledFallback, weight))
            return BundledFallback;

        if (IsUsable(BundledFallback, FontWeight.Normal))
            return BundledFallback;

        if (IsUsable(SystemFallback, weight))
            return SystemFallback;

        return FontManager.Current.DefaultFontFamily;
    }

    private static bool IsUsable(FontFamily? family, FontWeight weight)
    {
        if (family is null) return false;
        if (!IsConcreteFontFamily(family)) return false;

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