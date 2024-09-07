using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BetterRaid.Converters;

public class ChannelOnlineColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
        {
            return isOnline ? new SolidColorBrush(Colors.GreenYellow) : new SolidColorBrush(Colors.OrangeRed);
        }

        return new SolidColorBrush(Colors.OrangeRed);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color == Colors.GreenYellow;
        }
        
        return false;
    }
}