using System;
using System.Reactive;
using ReactiveUI;

namespace BetterRaid.ViewModels;

public class SettingsPageViewModel : ViewModelBase, IRoutableViewModel
{
    public string? UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen HostScreen { get; }
    public ReactiveCommand<Unit, IRoutableViewModel?> GoPageBackCommand { get; }

    public SettingsPageViewModel(IScreen host)
    {
        HostScreen = host;

        GoPageBackCommand = HostScreen.Router.NavigateBack;
    }
}
