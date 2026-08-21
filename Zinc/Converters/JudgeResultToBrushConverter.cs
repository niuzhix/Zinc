using Avalonia.Data.Converters;
using Avalonia.Media;
using Zinc.Core.Models;
using System.Globalization;
using System;
using Avalonia;
using Avalonia.Controls;

namespace Zinc.Converters;

public class JudgeResultToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JudgeResult result)
        {
            return result switch
            {
                JudgeResult.AC => Application.Current.FindResource("ACBrush") as IBrush,
                JudgeResult.WA => Application.Current.FindResource("WABrush") as IBrush,
                JudgeResult.TLE => Application.Current.FindResource("TLEBrush") as IBrush,
                JudgeResult.RE => Application.Current.FindResource("REBrush") as IBrush,
                JudgeResult.CE => Application.Current.FindResource("CEBrush") as IBrush,
                _ => Application.Current.FindResource("UKBrush") as IBrush
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}