using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace BetterRaid.Extensions;

public static class DataContextExtensions
{
    public static T? GetDataContextAs<T>(this T obj) where T : StyledElement
    {
        return obj.DataContext as T;
    }
    
    public static void InjectDataContext<T>(this StyledElement e) where T : class
    {
        if (Application.Current is not App { Provider: not null } app)
            return;
        
        var vm = app.Provider.GetRequiredService<T>();
        e.DataContext = vm;
    }
}