using Avalonia;
using Avalonia.Controls;
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

    private void OnAddChannelPanelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (e.Property != IsVisibleProperty)
            return;

        if (border.IsVisible is false)
            return;

        // direct access results in NullReferenceException - don't ask me why
        border.FindControl<TextBox>("ChannelNameTextBox")?.Focus();
    }
}