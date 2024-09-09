using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BetterRaid.ViewModels;
using ReactiveUI;

namespace BetterRaid.Views;

public partial class MainWindowChannelsListView : ReactiveUserControl<MainWindowChannelsListViewModel>
{
    public MainWindowChannelsListView()
    {
        this.WhenActivated(_ => { });
        AvaloniaXamlLoader.Load(this);
    }
}