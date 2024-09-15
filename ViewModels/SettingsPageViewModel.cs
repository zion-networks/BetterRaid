using System;
using System.Reactive;
using BetterRaid.Services;
using ReactiveUI;

namespace BetterRaid.ViewModels;

public class SettingsPageViewModel : ViewModelBase, IRoutableViewModel
{
    public string? UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen HostScreen { get; }
    public IDatabaseService Database { get; set; }
    public ReactiveCommand<Unit, IRoutableViewModel?> GoPageBackCommand { get; }

    public SettingsPageViewModel(IScreen host, IDatabaseService database)
    {
        HostScreen = host;
        Database = database;

        GoPageBackCommand = HostScreen.Router.NavigateBack;
    }

}
