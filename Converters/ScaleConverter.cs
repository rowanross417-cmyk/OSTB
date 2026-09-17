using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace OSTB.Converters;

/// <summary>
/// Converts a base value by multiplying it with AppScale.
/// Usage: {Binding AppScale, Converter={StaticResource ScaleConverter}, ConverterParameter=14}
/// </summary>
public class ScaleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not double scale || parameter is not string paramStr)
            return parameter;

        if (!double.TryParse(paramStr, CultureInfo.InvariantCulture, out double baseValue))
            return baseValue;

        return baseValue * scale;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        => throw new NotSupportedException();
}
