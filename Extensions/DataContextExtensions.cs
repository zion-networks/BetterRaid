using Avalonia.Controls;

namespace BetterRaid.Extensions;

public static class DataContextExtensions
{
    public static T? GetDataContextAs<T>(this T obj) where T : Window
    {
        return obj.DataContext as T;
    }
}