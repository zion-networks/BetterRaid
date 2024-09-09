using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    private readonly IDispatcherService _synchronizationService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IWebToolsService _webTools;
    private readonly IDatabaseService _db;
    private readonly ITwitchService _twitch;

    private string _filter;
    private bool _onlyOnline;
    private readonly ReadOnlyObservableCollection<TwitchChannel> _filteredChannels;
    private TwitchChannel? _selectedChannel;
    private bool _isAddChannelPopupVisible;
    private string _newChannelName;

    public ITwitchService Twitch => _twitch;
    public ILogger<MainWindowViewModel> Logger => _logger;

    public ReadOnlyObservableCollection<TwitchChannel> FilteredChannels =>
        _filteredChannels;

    public string Filter
    {
        get =>
            _filter;
        set
        {
            this.RaiseAndSetIfChanged(ref _filter,
                value);

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
        get =>
            _onlyOnline;
        set
        {
            this.RaiseAndSetIfChanged(ref _onlyOnline,
                value);

            _sourceList.Edit(innerList =>
            {
                if (_db.Database == null)
                    return;

                innerList.Clear();
                innerList.AddRange(_db.Database.Channels);
            });
        }
    }

    public bool IsAddChannelPopupVisible
    {
        get => _isAddChannelPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _isAddChannelPopupVisible, value);
    }

    public bool IsLoggedIn =>
        Twitch.UserChannel != null;

    public TwitchChannel? SelectedChannel
    {
        get => _selectedChannel;
        set => this.RaiseAndSetIfChanged(ref _selectedChannel, value);
    }

    public string NewChannelName
    {
        get => _newChannelName;
        set => this.RaiseAndSetIfChanged(ref _newChannelName, value);
    }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        ITwitchService twitch,
        IWebToolsService webTools,
        IDatabaseService db,
        IDispatcherService synchronizationService)
    {
        _logger = logger;
        _twitch = twitch;
        _webTools = webTools;
        _db = db;
        _synchronizationService = synchronizationService;
        _filter = string.Empty;

        Twitch.UserLoginChanged += OnUserLoginChanged;
        Twitch.TwitchChannelUpdated += OnTwitchChannelUpdated;

        _sourceList = new SourceList<TwitchChannel>();
        _sourceList.Connect()
            .Filter(channel => channel.Name.Contains(_filter,
                StringComparison.OrdinalIgnoreCase))
            .Filter(channel => !OnlyOnline || channel.IsLive)
            .Sort(SortExpressionComparer<TwitchChannel>
                .Descending(channel => channel.ViewerCount)
                .ThenByAscending(channel => channel.DisplayName!))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _filteredChannels)
            .Subscribe();

        InitializeChannels();
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
        _webTools.StartOAuthLogin(Twitch,
            OnTwitchLoginCallback,
            CancellationToken.None);
    }

    private void OnTwitchLoginCallback()
    {
        this.RaisePropertyChanged(nameof(IsLoggedIn));
    }

    private void InitializeChannels()
    {
        if (_db.Database == null)
        {
            Logger.LogError("Database is null");
            return;
        }

        Logger.LogDebug("Initializing {ChannelCount} channels", _db.Database.Channels.Count);
        var updateTasks = _db.Database.Channels.Select(c =>
        {
            return Task.Run(() =>
            {
                c.UpdateChannelData(Twitch);
                Twitch.RegisterForEvents(c);
            });
        });

        Task.WhenAll(updateTasks).ContinueWith(_ =>
        {
            Logger.LogDebug("Finished initializing channels");
            ReloadChannels();
        });
    }

    private void ReloadChannels()
    {
        if (_db.Database == null)
        {
            _logger.LogError("Database is null");
            return;
        }
        
        _sourceList.Edit(innerList =>
        {
            innerList.Clear();
            innerList.AddRange(_db.Database.Channels);
        });
    }
    
    public void RemoveChannel(TwitchChannel channel)
    {
        if (_db.Database == null)
        {
            _logger.LogError("Database is null");
            return;
        }
        
        Twitch.UnregisterFromEvents(channel);
        _db.Database.Channels.Remove(channel);
        
        if (_db.AutoSave)
            _db.Save();
        
        ReloadChannels();
    }

    public void ShowAddChannelPopup(bool show)
    {
        IsAddChannelPopupVisible = show;
    }
    
    public void AddChannel(string channelName)
    {
        if (_db.Database == null)
        {
            _logger.LogError("Database is null");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(channelName))
            return;
        
        if (_db.Database.Channels.Any(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase)))
            return;

        var channel = new TwitchChannel(channelName);
        channel.UpdateChannelData(Twitch);
        Twitch.RegisterForEvents(channel);
        
        _db.Database.Channels.Add(channel);

        if (_db.AutoSave)
            _db.Save();
        
        ReloadChannels();
        
        IsAddChannelPopupVisible = false;
        NewChannelName = string.Empty;
    }

    private void OnUserLoginChanged(object? sender,
        EventArgs e)
    {
        this.RaisePropertyChanged(nameof(IsLoggedIn));
    }

    private void OnTwitchChannelUpdated(object? sender, TwitchChannel e)
    {
        ReloadChannels();
    }
}