using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using BetterRaid.Extensions;
using BetterRaid.Models;
using BetterRaid.Services;
using BetterRaid.Views;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BetterRaid.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly SourceList<TwitchChannel> _sourceList;

    private readonly ISynchronizaionService _synchronizationService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IWebToolsService _webTools;
    private readonly IDatabaseService _db;
    private readonly ITwitchService _twitch;

    private string _filter;
    private bool _onlyOnline;
    private readonly ReadOnlyObservableCollection<TwitchChannel> _filteredChannels;

    public ITwitchService Twitch => _twitch;

    public ReadOnlyObservableCollection<TwitchChannel> FilteredChannels => _filteredChannels;

    public string Filter
    {
        get => _filter;
        set
        {
            this.RaiseAndSetIfChanged(ref _filter, value);
            
            _sourceList.Edit(innerList =>
            {
                if (_db.Database == null)
                    return;
                
                innerList.Clear();
                innerList.AddRange(_db.Database.Channels);
            });
        }
    }

    public bool OnlyOnline
    {
        get => _onlyOnline;
        set
        {
            this.RaiseAndSetIfChanged(ref _onlyOnline, value);
            
            _sourceList.Edit(innerList =>
            {
                if (_db.Database == null)
                    return;
                
                innerList.Clear();
                innerList.AddRange(_db.Database.Channels);
            });
        }
    }

    public bool IsLoggedIn => _twitch.UserChannel != null;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        ITwitchService twitch,
        IWebToolsService webTools,
        IDatabaseService db,
        ISynchronizaionService synchronizationService)
    {
        _logger = logger;
        _twitch = twitch;
        _webTools = webTools;
        _db = db;
        _synchronizationService = synchronizationService;
        _filter = string.Empty;

        _twitch.UserLoginChanged += OnUserLoginChanged;

        _sourceList = new SourceList<TwitchChannel>();
        _sourceList.Connect()
            .Filter(channel => channel.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
            .Filter(channel => !OnlyOnline || channel.IsLive)
            .Sort(SortExpressionComparer<TwitchChannel>.Descending(channel => channel.IsLive))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _filteredChannels)
            .Subscribe();

        LoadChannelsFromDb();
    }

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
        _webTools.StartOAuthLogin(_twitch, OnTwitchLoginCallback, CancellationToken.None);
    }

    private void OnTwitchLoginCallback()
    {
        this.RaisePropertyChanged(nameof(IsLoggedIn));
    }

    private void LoadChannelsFromDb()
    {
        if (_db.Database == null)
        {
            _logger.LogError("Database is null");
            return;
        }

        foreach (var channel in _db.Database.Channels)
        {
            Task.Run(() =>
            {
                channel.UpdateChannelData(_twitch);
                _twitch.RegisterForEvents(channel);
            });
        }
        
        _sourceList.Edit(innerList => innerList.AddRange(_db.Database.Channels));
    }

    private void OnUserLoginChanged(object? sender, EventArgs e)
    {
        this.RaisePropertyChanged(nameof(IsLoggedIn));
    }
}