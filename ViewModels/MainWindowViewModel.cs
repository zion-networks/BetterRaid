using System;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Controls;
using BetterRaid.Extensions;
using BetterRaid.Models;
using BetterRaid.Services;
using BetterRaid.Views;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BetterRaid.ViewModels;

public class MainWindowViewModel : ViewModelBase, IScreen
{
    private string _filter;
    private bool _onlyOnline;
    private string _newChannelName;

    #region Services
    
    private ILogger<MainWindowViewModel> Logger { get; }
    private ITwitchService Twitch { get; }
    private IWebToolsService WebTools { get; }
    private IDatabaseService Database { get; }

    #endregion
    
    public RoutingState Router { get; } = new();
    public MainWindowChannelsListViewModel ChannelsListVm { get; set; }

    public string Filter
    {
        get => _filter;
        set => this.RaiseAndSetIfChanged(ref _filter, value);
    }

    public bool OnlyOnline
    {
        get => _onlyOnline;
        set => this.RaiseAndSetIfChanged(ref _onlyOnline, value);
    }

    public bool IsLoggedIn =>
        Twitch.UserChannel != null;

    public string NewChannelName
    {
        get => _newChannelName;
        set => this.RaiseAndSetIfChanged(ref _newChannelName, value);
    }

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        ILogger<MainWindowViewModel> logger,
        ITwitchService twitch,
        IWebToolsService webTools,
        IDatabaseService db)
    {
        Logger = logger;
        Twitch = twitch;
        WebTools = webTools;
        Database = db;
        Filter = string.Empty;
        
        Twitch.UserLoginChanged += OnUserLoginChanged;
        
        var mwclvmLogger = serviceProvider.GetRequiredService<ILogger<MainWindowChannelsListViewModel>>();
        ChannelsListVm = new MainWindowChannelsListViewModel(mwclvmLogger, this, Database, Twitch);
        Router.Navigate.Execute(ChannelsListVm);
    }

    private void OnUserLoginChanged(object? sender, EventArgs e)
    {
        this.RaisePropertyChanged(nameof(IsLoggedIn));
    }

    public void ExitApplication()
    {
        //TODO polish later
        Environment.Exit(0);
    }

    public void LoginWithTwitch()
    {
        WebTools.StartOAuthLogin(Twitch,
            OnTwitchLoginCallback,
            CancellationToken.None);
    }

    private void OnTwitchLoginCallback()
    {
        this.RaisePropertyChanged(nameof(IsLoggedIn));
    }
}