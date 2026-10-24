using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Zinc.Models;

namespace Zinc.Services;

/// <summary>
/// 编辑器字体的可用性判定与回退解析。与具体 ViewModel 解耦，
/// 供设置页显示与应用启动时校正共用。
/// </summary>
public static class EditorFontResolver
{
    private static readonly FontFamily BundledFallback =
        new("avares://Zinc/Assets/Fonts#Fira Code Light");

    private static readonly FontFamily SystemFallback = new("Consolas");

    private static readonly Lazy<IReadOnlyList<FontFamily>> CachedSystemFonts =
        new(LoadSystemFonts, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyList<FontWeight> AvailableWeights { get; } = new[]
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

    /// <summary>系统字体列表（进程内只枚举一次）。</summary>
    public static IReadOnlyList<FontFamily> SystemFonts => CachedSystemFonts.Value;

    public static FontWeight NormalizeWeight(FontWeight weight)
        => AvailableWeights.Contains(weight) ? weight : FontWeight.Normal;

    /// <summary>把持久化的字体/字重校正为确定可用的值；仅在确有变化时写入设置。</summary>
    public static void EnsureUsable(AppSettings settings)
    {
        var weight = NormalizeWeight(settings.EditorFontWeight);
        var font = Resolve(settings.EditorFont, weight);

        settings.EditorFontWeight = weight;
        settings.EditorFont = font;
    }

    public static FontFamily Resolve(FontFamily? preferred, FontWeight weight)
    {
        if (preferred is not null && IsUsable(preferred, weight))
            return preferred;

        if (IsUsable(BundledFallback, weight))
            return BundledFallback;

        if (IsUsable(BundledFallback, FontWeight.Normal))
            return BundledFallback;

        if (IsUsable(SystemFallback, weight))
            return SystemFallback;

        return FontManager.Current.DefaultFontFamily;
    }

    public static bool IsUsable(FontFamily? family, FontWeight weight)
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

    private static bool IsConcreteFontFamily(FontFamily family)
    {
        if (family is null) return false;

        if (family.Key?.Source == FontManager.Current.SystemFonts.Key) return false;

        if (family.Name?.StartsWith("fonts:", StringComparison.OrdinalIgnoreCase) == true)
            return false;

        return !string.IsNullOrWhiteSpace(family.Name);
    }

    private static IReadOnlyList<FontFamily> LoadSystemFonts()
        => FontManager.Current.SystemFonts
            .Where(IsConcreteFontFamily)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
