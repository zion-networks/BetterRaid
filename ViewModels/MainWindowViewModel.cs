using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using BetterRaid.Extensions;
using BetterRaid.Misc;
using BetterRaid.Models;
using BetterRaid.Services;
using BetterRaid.Views;

namespace BetterRaid.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string? _filter;
    private ObservableCollection<TwitchChannel> _channels = [];
    private readonly BetterRaidDatabase? _db;
    private readonly ITwitchPubSubService _pubSub;
    private readonly ISynchronizaionService _synchronizaionService;

    public BetterRaidDatabase? Database
    {
        get => _db;
        private init
        {
            if (SetProperty(ref _db, value) && _db != null)
            {
                LoadChannelsFromDb();
            }
        }
    }

    public ObservableCollection<TwitchChannel> Channels
    {
        get => _channels;
        set => SetProperty(ref _channels, value);
    }

    public ObservableCollection<TwitchChannel> FilteredChannels => GetFilteredChannels();

    public ITwitchDataService DataService { get; }

    public string? Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public bool IsLoggedIn => DataService.UserChannel != null;

    public MainWindowViewModel(ITwitchPubSubService pubSub, ITwitchDataService dataService, ISynchronizaionService synchronizaionService)
    {
        _pubSub = pubSub;
        DataService = dataService;
        _synchronizaionService = synchronizaionService;
        DataService.PropertyChanged += OnDataServicePropertyChanged;

        Database = BetterRaidDatabase.LoadFromFile(Constants.DatabaseFilePath);
        Database.PropertyChanged += OnDatabasePropertyChanged;
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
        Tools.StartOAuthLogin(DataService.GetOAuthUrl(), DataService, OnTwitchLoginCallback, CancellationToken.None);
    }

    private void OnTwitchLoginCallback()
    {
        OnPropertyChanged(nameof(IsLoggedIn));
    }

    private void LoadChannelsFromDb()
    {
        if (_db == null)
        {
            return;
        }

        foreach (var channel in Channels)
        {
            _pubSub.UnregisterReceiver(channel);
        }

        Channels.Clear();

        var channels = _db.Channels
            .Select(channelName => new TwitchChannel(channelName))
            .ToList();

        foreach (var channel in channels)
        {
            Task.Run(() =>
            {
                channel.UpdateChannelData(DataService);
                _pubSub.RegisterReceiver(channel);
            });

            Channels.Add(channel);
        }
    }

    private ObservableCollection<TwitchChannel> GetFilteredChannels()
    {
        var filteredChannels = Channels
            .Where(channel => Database?.OnlyOnline == false || channel.IsLive)
            .Where(channel => string.IsNullOrWhiteSpace(Filter) || channel.Name?.Contains(Filter, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        return new ObservableCollection<TwitchChannel>(filteredChannels);
    }

    private void OnDataServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DataService.UserChannel))
            return;

        OnPropertyChanged(nameof(IsLoggedIn));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(Filter))
        {
            OnPropertyChanged(nameof(FilteredChannels));
        }
    }

    private void OnDatabasePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BetterRaidDatabase.OnlyOnline))
            return;

        OnPropertyChanged(nameof(FilteredChannels));
    }
}
