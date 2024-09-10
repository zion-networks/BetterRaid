using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BetterRaid.ViewModels;
using ReactiveUI;

namespace BetterRaid.Controls;

public partial class SettingsPage : ReactiveUserControl<SettingsPageViewModel>
{
    public SettingsPage()
    {
        this.WhenActivated(_ => { });
        AvaloniaXamlLoader.Load(this);
    }
}