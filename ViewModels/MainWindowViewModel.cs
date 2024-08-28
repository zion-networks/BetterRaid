using System;
using System.Threading;
using Avalonia.Controls;
using BetterRaid.Extensions;
using BetterRaid.Misc;
using BetterRaid.Models;
using BetterRaid.Views;

namespace BetterRaid.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string? _filter;
    
    private BetterRaidDatabase? _db;

    public BetterRaidDatabase? Database
    {
        get => _db;
        set => SetProperty(ref _db, value);
    }

    public string? Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public bool IsLoggedIn => App.TwitchApi != null;

    public void ExitApplication()
    {
        //TODO polish later
        Environment.Exit(0);
    }

    public void ShowAboutWindow(Window owner)
    {
        var about = new AboutWindow();
        about.ShowDialog(owner);
        about.CenterToOwner();
    }

    public void LoginWithTwitch()
    {
        Tools.StartOAuthLogin(App.TwitchOAuthUrl, OnTwitchLoginCallback, CancellationToken.None);
    }

    public void OnTwitchLoginCallback()
    {
        App.InitTwitchClient(overrideToken: true);

        OnPropertyChanged(nameof(IsLoggedIn));
    }
}
