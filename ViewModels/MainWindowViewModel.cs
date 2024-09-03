using System;
using System.Collections.ObjectModel;
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

public partial class MainWindowViewModel : ViewModelBase
{
    private string? _filter;
    private ObservableCollection<TwitchChannel> _channels = [];
    private BetterRaidDatabase? _db;
    private readonly ITwitchPubSubService _pubSub;

    public BetterRaidDatabase? Database
    {
        get => _db;
        set
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
    
    public ITwitchDataService DataService { get; }

    public string? Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public bool IsLoggedIn => DataService.UserChannel != null;

    public MainWindowViewModel(ITwitchPubSubService pubSub, ITwitchDataService dataService)
    {
        _pubSub = pubSub;
        DataService = dataService;

        Database = BetterRaidDatabase.LoadFromFile(Constants.DatabaseFilePath);
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
        Tools.StartOAuthLogin(DataService.GetOAuthUrl(), OnTwitchLoginCallback, CancellationToken.None);
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
}
