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
            ChannelsPageViewModel ctx => new ChannelsPage { DataContext = ctx },
            MainWindowViewModel ctx => new MainWindow { DataContext = ctx },
            SettingsPageViewModel ctx => new SettingsPage { DataContext = ctx },
            _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
        };
    }
}