using System;
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
            MainWindowChannelsListViewModel context => new MainWindowChannelsListView { DataContext = context },
            _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
        };
    }
}