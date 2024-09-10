using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BetterRaid.ViewModels;
using ReactiveUI;

namespace BetterRaid.Controls;

public partial class ChannelsListControl : ReactiveUserControl<ChannelsListViewModel>
{
    public ChannelsListControl()
    {
        this.WhenActivated(_ => { });
        AvaloniaXamlLoader.Load(this);
    }
}