using System;
using BetterRaid.Controls;
using BetterRaid.ViewModels;
using BetterRaid.Views;
using ReactiveUI;

namespace BetterRaid;

public class RxViewLocator : IViewLocator
{
    public IViewFor ResolveView<T>(T? viewModel, string? contract = null)
    {
        ArgumentNullException.ThrowIfNull(viewModel, nameof(viewModel));
        
        return viewModel switch
        {
            ChannelsListViewModel context => new ChannelsListControl { DataContext = context },
            MainWindowViewModel context => new MainWindow { DataContext = context },
            _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
        };
    }
}