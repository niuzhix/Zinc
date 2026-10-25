using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using Zinc.Core.Models;

namespace Zinc.Converters;

public sealed class JudgeResultToBrushConverter : IValueConverter
{
    private const string FallbackBrushKey = "UKEBrush";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resourceKey = value switch
        {
            JudgeResult.AC => "ACBrush",
            JudgeResult.WA => "WABrush",
            JudgeResult.TLE => "TLEBrush",
            JudgeResult.MLE => "MLEBrush",
            JudgeResult.RE => "REBrush",
            JudgeResult.CE => "CEBrush",
            _ => FallbackBrushKey
        };

        if (Application.Current is { } app
            && app.TryFindResource(resourceKey, out object? resource)
            && resource is IBrush brush)
        {
            return brush;
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException($"{nameof(JudgeResultToBrushConverter)} 不支持反向转换。");
}
