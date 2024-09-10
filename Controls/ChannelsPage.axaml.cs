using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BetterRaid.ViewModels;
using ReactiveUI;

namespace BetterRaid.Controls;

public partial class ChannelsPage : ReactiveUserControl<ChannelsPageViewModel>
{
    public ChannelsPage()
    {
        this.WhenActivated(_ => { });
        AvaloniaXamlLoader.Load(this);
    }
}