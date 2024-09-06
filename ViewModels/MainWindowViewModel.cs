using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using BetterRaid.Extensions;
using BetterRaid.Models;
using BetterRaid.Services;
using BetterRaid.Views;
using Microsoft.Extensions.Logging;

namespace BetterRaid.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<TwitchChannel> _channels = [];
    private readonly ISynchronizaionService _synchronizationService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IWebToolsService _webTools;
    private readonly IDatabaseService _db;
    private readonly ITwitchService _twitch;
    
    private string? _filter;
    private bool _onlyOnline;
    
    public ITwitchService Twitch => _twitch;

    public ObservableCollection<TwitchChannel> Channels
    {
        get => _channels;
        set => SetProperty(ref _channels, value);
    }

    public string? Filter
    {
        get => _filter;
        set
        {
            SetProperty(ref _filter, value);
            LoadChannelsFromDb();
        }
    }

    public bool OnlyOnline
    {
        get => _db.OnlyOnline;
        set
        {
            SetProperty(ref _onlyOnline, value);
            LoadChannelsFromDb();
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
        
        _twitch.UserLoginChanged += OnUserLoginChanged;
        _twitch.TwitchChannelUpdated += OnTwitchChannelUpdated;
        
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
        OnPropertyChanged(nameof(IsLoggedIn));
    }

    private void LoadChannelsFromDb()
    {
        if (_db.Database == null)
        {
            _logger.LogError("Database is null");
            return;
        }
        
        foreach (var channel in Channels)
        {
            _twitch.UnregisterFromEvents(channel);
        }

        Channels.Clear();
        
        var channels = _db.Database.Channels
            .ToList()
            .OrderByDescending(c => c.IsLive)
            .Where(c => OnlyOnline && c.IsLive || !OnlyOnline)
            .Where(c => string.IsNullOrWhiteSpace(Filter) || c.Name?.Contains(Filter, StringComparison.CurrentCultureIgnoreCase) == true)
            .ToList();
        
        foreach (var channel in channels)
        {
            Task.Run(() =>
            {
                channel.UpdateChannelData(_twitch);
                _twitch.RegisterForEvents(channel);
            });

            Channels.Add(channel);
        }
    }

    private void OnTwitchChannelUpdated(object? sender, TwitchChannel channel)
    {
        LoadChannelsFromDb();
    }

    private void OnUserLoginChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsLoggedIn));
    }
}
