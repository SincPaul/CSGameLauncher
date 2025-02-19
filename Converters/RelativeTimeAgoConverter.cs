using System;
using System.Globalization;
using Avalonia.Data.Converters;
using GameLauncher.Functions;

namespace GameLauncher.Converters;

public class RelativeTimeAgoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not int timestamp ? "Unknown" : TimeUtils.RelativeTimeAgoConverter(timestamp);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}