using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterRaid.Models;
using BetterRaid.Services;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BetterRaid.ViewModels;

public class ChannelsPageViewModel : ViewModelBase, IRoutableViewModel
{
    #region Private Fields
    
    private readonly SourceList<TwitchChannel> _sourceList;
    private readonly ReadOnlyObservableCollection<TwitchChannel> _filteredChannels;
    private TwitchChannel? _selectedChannel;
    private bool _isAddChannelPopupVisible;
    private string _newChannelName;
    private Task _updateChannelsListTask;
    private CancellationTokenSource _updateChannelsListCts = new();
    
    #endregion Private Fields
    
    #region Services
    
    private ILogger<ChannelsPageViewModel> Logger { get; set; }
    public MainWindowViewModel? MainVm { get; set; }
    public ITwitchService Twitch { get; set; }
    public IDatabaseService Database { get; set; }
    public IWebToolsService WebTools { get; set; }
    
    #endregion Services
    
    #region Reactive Commands
    
    public ReactiveCommand<TwitchChannel, Unit> RemoveChannelCommand { get; }
    public ReactiveCommand<bool, Unit> ShowAddChannelPopupCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSettingsPageCommand { get; }
    public ReactiveCommand<TwitchChannel, Unit> VisitChannelCommand { get; }

    
    #endregion Reactive Commands
    
    #region Public Properties
    
    public bool IsInitialising { get; private set; } = true;
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

    public ChannelsPageViewModel
        (
            ILogger<ChannelsPageViewModel> logger,
            MainWindowViewModel mainVm,
            IDatabaseService db,
            ITwitchService twitch,
            IWebToolsService webTools
        )
    {
        HostScreen = mainVm;
        MainVm = mainVm;
        Logger = logger;
        Database = db;
        Twitch = twitch;
        WebTools = webTools;
        
        Twitch.TwitchChannelUpdated += OnTwitchChannelUpdated;

        if (MainVm is null)
        {
            Logger.LogError("Failed to initialize {ViewModel} because {MainVm} is null",
                nameof(ChannelsPageViewModel), nameof(MainVm));
            return;
        }

        var filterObservable = this.WhenAnyValue(
            x => x.MainVm!.Filter,
            x => x.MainVm!.OnlyOnline,
            (filter, onlyOnline) => new { Filter = filter, OnlyOnline = onlyOnline });
        
        _sourceList = new SourceList<TwitchChannel>();
        _sourceList.Connect()
            .Filter(filterObservable.Select(f => (Func<TwitchChannel, bool>)(channel => channel.DisplayName?.Contains(
                    f.Filter,
                    StringComparison.OrdinalIgnoreCase) == true &&
                (!f.OnlyOnline || channel.IsLive))))
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
        
        ShowSettingsPageCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await MainVm.Router.Navigate.Execute(new SettingsPageViewModel(MainVm, Database));
        });
        
        VisitChannelCommand = ReactiveCommand.Create<TwitchChannel>(
            execute: channel => WebTools.OpenUrl($"https://twitch.tv/{channel.Name}"));

        _updateChannelsListTask = Task.Run(() =>
        {
            while (_updateChannelsListCts.IsCancellationRequested == false)
            {
                ReloadChannels();
                Task.Delay(10_000).Wait();
            }
        }, _updateChannelsListCts.Token);
    }


    ~ChannelsPageViewModel()
    {
        _updateChannelsListCts.Cancel();
    }

    private void InitializeChannels()
    {
        if (Database.Database == null)
        {
            Logger.LogError("Database is null");
            return;
        }

        Logger.LogDebug("Initializing {ChannelCount} channels", Database.Database.Channels.Count);
        
        Task.Run(() =>
        {
            return Twitch
                .LoadChannelDataAsync(Database.Database.Channels)
                .ContinueWith(_ =>
                {
                    IsInitialising = false;
                    ReloadChannels();
                })
                .ContinueWith(_ => Twitch.RegisterForEventsAsync(Database.Database.Channels));
        });
    }

    private void ReloadChannels()
    {
        if (IsInitialising)
            return;
        
        if (Database.Database == null)
        {
            Logger.LogError("Database is null");
            return;
        }
        
        Logger.LogDebug("Reloading channels");
        
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
        //ReloadChannels();
    }
}