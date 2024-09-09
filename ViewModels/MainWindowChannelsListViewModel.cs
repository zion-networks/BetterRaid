using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BetterRaid.Models;
using BetterRaid.Services;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BetterRaid.ViewModels;

public class MainWindowChannelsListViewModel : ViewModelBase, IRoutableViewModel
{
    #region Private Fields
    
    private readonly SourceList<TwitchChannel> _sourceList;
    private readonly ReadOnlyObservableCollection<TwitchChannel> _filteredChannels;
    private TwitchChannel? _selectedChannel;
    private bool _isAddChannelPopupVisible;
    private string _newChannelName;
    
    #endregion Private Fields
    
    #region Services
    
    private ILogger<MainWindowChannelsListViewModel> Logger { get; set; }
    private MainWindowViewModel? MainVm { get; set; }
    public ITwitchService Twitch { get; set; }
    public IDatabaseService Database { get; set; }
    
    #endregion Services
    
    #region Reactive Commands
    
    public ReactiveCommand<TwitchChannel, Unit> RemoveChannelCommand { get; }
    public ReactiveCommand<bool, Unit> ShowAddChannelPopupCommand { get; }
    
    #endregion Reactive Commands
    
    #region Public Properties
    
    public string? UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);
    public IScreen HostScreen { get; }
    
    public ReadOnlyObservableCollection<TwitchChannel> FilteredChannels =>
        _filteredChannels;
    
    public TwitchChannel? SelectedChannel
    {
        get => _selectedChannel;
        set => this.RaiseAndSetIfChanged(ref _selectedChannel, value);
    }
    
    public bool IsAddChannelPopupVisible
    {
        get => _isAddChannelPopupVisible;
        set => this.RaiseAndSetIfChanged(ref _isAddChannelPopupVisible, value);
    }
    
    public string NewChannelName
    {
        get => _newChannelName;
        set => this.RaiseAndSetIfChanged(ref _newChannelName, value);
    }
    
    #endregion Public Properties
    
    public MainWindowChannelsListViewModel
        (
            ILogger<MainWindowChannelsListViewModel> logger,
            MainWindowViewModel mainVm,
            IDatabaseService db,
            ITwitchService twitch
        )
    {
        HostScreen = mainVm;
        MainVm = mainVm;
        Logger = logger;
        Database = db;
        Twitch = twitch;
        
        Twitch.TwitchChannelUpdated += OnTwitchChannelUpdated;

        if (MainVm is null)
        {
            Logger.LogError("Failed to initialize {ViewModel} because {MainVm} is null",
                nameof(MainWindowChannelsListViewModel), nameof(MainVm));
            return;
        }
        
        _sourceList = new SourceList<TwitchChannel>();
        _sourceList.Connect()
            .Filter(channel => channel.Name.Contains(MainVm.Filter,
                StringComparison.OrdinalIgnoreCase))
            .Filter(channel => !MainVm.OnlyOnline || channel.IsLive)
            .Sort(SortExpressionComparer<TwitchChannel>
                .Descending(channel => channel.ViewerCount)
                .ThenByAscending(channel => channel.DisplayName!))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _filteredChannels)
            .Subscribe();

        InitializeChannels();
        
        RemoveChannelCommand = ReactiveCommand.Create<TwitchChannel>(
            execute: RemoveChannel,
            canExecute: this.WhenAny(x => x.Database.Database, x => x.Value != null)
        );
        
        ShowAddChannelPopupCommand = ReactiveCommand.Create<bool>(
            execute: ShowAddChannelPopup,
            canExecute: this.WhenAny(x => x.Database.Database, x => x.Value != null)
        );
    }

    private void InitializeChannels()
    {
        if (Database.Database == null)
        {
            Logger.LogError("Database is null");
            return;
        }

        Logger.LogDebug("Initializing {ChannelCount} channels", Database.Database.Channels.Count);
        var updateTasks = Database.Database.Channels.Select(c =>
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
        if (Database.Database == null)
        {
            Logger.LogError("Database is null");
            return;
        }
        
        _sourceList.Edit(innerList =>
        {
            innerList.Clear();
            innerList.AddRange(Database.Database.Channels);
        });
    }
    
    public void RemoveChannel(TwitchChannel channel)
    {
        if (Database.Database == null)
        {
            Logger.LogError("Database is null");
            return;
        }
        
        Twitch.UnregisterFromEvents(channel);
        Database.Database.Channels.Remove(channel);
        
        if (Database.AutoSave)
            Database.Save();
        
        ReloadChannels();
    }

    public void ShowAddChannelPopup(bool show)
    {
        IsAddChannelPopupVisible = show;
    }
    
    public void AddChannel(string channelName)
    {
        if (Database.Database == null)
        {
            Logger.LogError("Database is null");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(channelName))
            return;
        
        if (Database.Database.Channels.Any(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase)))
            return;

        var channel = new TwitchChannel(channelName);
        channel.UpdateChannelData(Twitch);
        Twitch.RegisterForEvents(channel);
        
        Database.Database.Channels.Add(channel);

        if (Database.AutoSave)
            Database.Save();
        
        ReloadChannels();
        
        IsAddChannelPopupVisible = false;
        NewChannelName = string.Empty;
    }

    private void OnTwitchChannelUpdated(object? sender, TwitchChannel e)
    {
        ReloadChannels();
    }
}