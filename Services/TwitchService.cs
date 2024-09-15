using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BetterRaid.Misc;
using BetterRaid.Models;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace BetterRaid.Services;

public interface ITwitchService
{
    public string AccessToken { get; }
    public TwitchChannel? UserChannel { get; set; }
    public TwitchAPI TwitchApi { get; }
    public bool IsRaidStarted { get; set; }
    public double RaidTimeProgress { get; }
    public int RaidParticipants { get; }
    public TwitchChannel? RaidedChannel { get; set; }
    
    public Task ConnectApiAsync(string clientId, string accessToken);
    public void SaveAccessToken();
    public bool CanStartRaidCommand(TwitchChannel? channel);
    public void StartRaidCommand(TwitchChannel? channel);
    public void StopRaid();
    public void StopRaidCommand();
    public void OpenChannelCommand(TwitchChannel? channel);
    public void RegisterForEvents(TwitchChannel channel);
    public Task RegisterForEventsAsync(TwitchChannel channel);
    public void RegisterForEvents(IEnumerable<TwitchChannel> channels);
    public Task RegisterForEventsAsync(IEnumerable<TwitchChannel> channels);
    public Task LoadChannelDataAsync(TwitchChannel channel);
    Task LoadChannelDataAsync(List<TwitchChannel> channels);
    public void UnregisterFromEvents(TwitchChannel channel);

    public event EventHandler<EventArgs>? UserLoginChanged;
    public event EventHandler<TwitchChannel>? TwitchChannelUpdated;
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class TwitchService : ITwitchService, INotifyPropertyChanged, INotifyPropertyChanging
{
    private bool _isRaidStarted;
    private DateTime _raidStartTime;
    private double _raidTimeProgress;
    private TwitchChannel? _raidedChannel;
    private int _raidParticipants;
    private TwitchChannel? _userChannel;
    private User? _user;
    private readonly ILogger<TwitchService> _logger;
    private readonly IWebToolsService _webTools;

    public string AccessToken { get; private set; } = string.Empty;

    public bool IsRaidStarted
    {
        get => _isRaidStarted;
        set => SetField(ref _isRaidStarted, value);
    }

    public double RaidTimeProgress
    {
        get => _raidTimeProgress;
        set => SetField(ref _raidTimeProgress, value);
    }
    
    public TwitchChannel? RaidedChannel
    {
        get => _raidedChannel;
        set => SetField(ref _raidedChannel, value);
    }

    public int RaidParticipants
    {
        get => _raidParticipants;
        set => SetField(ref _raidParticipants, value);
    }

    public User? User
    {
        get => _user;
        set => SetField(ref _user, value);
    }

    public TwitchChannel? UserChannel
    {
        get => _userChannel;
        set
        {
            if (_userChannel != null && _userChannel.Id?.Equals(value?.Id) == true)
                return;
            
            if (SetField(ref _userChannel, value) is false)
                return;

            _userChannel = value;
            _userChannel?.UpdateChannelData(this);
            OnUserLoginChanged();
        }
    }

    private ILogger<TwitchChannel> ChannelLogger { get; set; }
    
    public TwitchAPI TwitchApi { get; }
    public TwitchPubSub TwitchEvents { get; }

    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<EventArgs>? UserLoginChanged;
    public event EventHandler<TwitchChannel>? TwitchChannelUpdated;

    public event EventHandler<OnStreamDownArgs> OnStreamDown
    {
        add => TwitchEvents.OnStreamDown += value;
        remove => TwitchEvents.OnStreamDown -= value;
    }

    public event EventHandler<OnStreamUpArgs> OnStreamUp
    {
        add => TwitchEvents.OnStreamUp += value;
        remove => TwitchEvents.OnStreamUp -= value;
    }

    public TwitchService(
        ILogger<TwitchService> logger,
        ILogger<TwitchChannel> channelLogger,
        IWebToolsService webTools)
    {
        _logger = logger;
        _webTools = webTools;
        ChannelLogger = channelLogger;

        TwitchApi = new TwitchAPI();
        TwitchEvents = new TwitchPubSub();

        if (TryLoadAccessToken(out var token))
        {
            _logger.LogInformation("Found access token.");
            
            // Called synchronously, to make sure everything is set up before the app starts
            ConnectApiAsync(Constants.TwitchClientId, token).Wait();
            ConnectTwitchEventsAsync().Wait();
        }
        else
        {
            _logger.LogInformation("No access token found.");
        }
    }

    public async Task ConnectApiAsync(string clientId, string accessToken)
    {
        _logger.LogInformation("Connecting to Twitch API ...");

        AccessToken = accessToken;

        TwitchApi.Settings.ClientId = clientId;
        TwitchApi.Settings.AccessToken = accessToken;

        if (TryGetUser(out var user))
        {
            User = user;
        }
        else
        {
            User = null;

            _logger.LogError(
                "Could not get user with client id {clientId} - please check your clientId and accessToken", clientId);
        }

        if (TryGetUserChannel(out var channel))
        {
            UserChannel = channel;
            _logger.LogInformation(
                "Connected to Twitch API as {channelName} with broadcaster id {channelBroadcasterId}.", channel?.Name,
                channel?.BroadcasterId);
        }
        else
        {
            UserChannel = null;

            _logger.LogError("Could not get user channel.");
        }

        if (User == null || UserChannel == null)
        {
            _logger.LogError("Could not connect to Twitch API.");
        }

        await Task.CompletedTask;
    }

    private async Task ConnectTwitchEventsAsync()
    {
        if (UserChannel == null || User == null)
        {
            _logger.LogError("Could not connect to Twitch Events: User or UserChannel is null.");
            return;
        }

        _logger.LogInformation("Connecting to Twitch Events ...");

        TwitchEvents.OnRaidGo += OnUserRaidGo;
        TwitchEvents.OnRaidUpdateV2 += OnUserRaidUpdate;
        TwitchEvents.OnStreamUp += OnUserStreamUp;
        TwitchEvents.OnStreamDown += OnUserStreamDown;
        TwitchEvents.OnViewCount += OnViewCount;
        TwitchEvents.OnLog += OnPubSubLog;
        TwitchEvents.OnPubSubServiceError += OnPubSubServiceError;
        TwitchEvents.OnPubSubServiceConnected += OnPubSubServiceConnected;
        TwitchEvents.OnPubSubServiceClosed += OnPubSubServiceClosed;

        TwitchEvents.ListenToVideoPlayback(UserChannel.BroadcasterId);
        TwitchEvents.ListenToRaid(UserChannel.BroadcasterId);

        TwitchEvents.Connect();
        
        await Task.CompletedTask;
    }

    private bool TryLoadAccessToken(out string token)
    {
        token = string.Empty;

        if (!File.Exists(Constants.TwitchOAuthAccessTokenFilePath))
            return false;

        token = File.ReadAllText(Constants.TwitchOAuthAccessTokenFilePath);
        return true;
    }

    public void SaveAccessToken()
    {
        File.WriteAllText(Constants.TwitchOAuthAccessTokenFilePath, AccessToken);
    }

    public bool TryGetUser(out User? user)
    {
        user = null;

        try
        {
            var userResult = TwitchApi.Helix.Users.GetUsersAsync().Result.Users[0];

            if (userResult == null)
            {
                return false;
            }

            user = userResult;
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not get user.");
            return false;
        }
    }

    public bool TryGetUserChannel(out TwitchChannel? channel)
    {
        channel = null;

        if (User == null)
            return false;

        channel = new TwitchChannel(User.Login, ChannelLogger);
        channel.UpdateChannelData(this);

        return true;
    }

    public void RegisterForEvents(TwitchChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel, nameof(channel));
        
        _logger.LogDebug("Registering for events for {channelName} with broadcaster id {channelBroadcasterId} ...",
            channel.Name, channel.BroadcasterId);

        channel.PropertyChanged += OnTwitchChannelUpdated;

        TwitchEvents.OnStreamUp += channel.OnStreamUp;
        TwitchEvents.OnStreamDown += channel.OnStreamDown;
        TwitchEvents.OnViewCount += channel.OnViewCount;

        TwitchEvents.ListenToVideoPlayback(channel.Id);

        TwitchEvents.SendTopics(AccessToken);
    }

    public async Task RegisterForEventsAsync(TwitchChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel, nameof(channel));

        await Task.Run(() =>
        {
            _logger.LogDebug("Registering for events for {channelName} with broadcaster id {channelBroadcasterId} ...",
                channel.Name, channel.BroadcasterId);

            channel.PropertyChanged += OnTwitchChannelUpdated;

            TwitchEvents.OnStreamUp += channel.OnStreamUp;
            TwitchEvents.OnStreamDown += channel.OnStreamDown;
            TwitchEvents.OnViewCount += channel.OnViewCount;

            TwitchEvents.ListenToVideoPlayback(channel.Id);
            return Task.CompletedTask;
        }).ContinueWith(_ => TwitchEvents.SendTopics(AccessToken));
    }

    public void RegisterForEvents(IEnumerable<TwitchChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(channels, nameof(channels));
        
        foreach (var channel in channels)
        {
            _logger.LogDebug("Registering for events for {channelName} with broadcaster id {channelBroadcasterId} ...",
                channel.Name, channel.BroadcasterId);

            channel.PropertyChanged += OnTwitchChannelUpdated;

            TwitchEvents.OnStreamUp += channel.OnStreamUp;
            TwitchEvents.OnStreamDown += channel.OnStreamDown;
            TwitchEvents.OnViewCount += channel.OnViewCount;

            TwitchEvents.ListenToVideoPlayback(channel.Id);
        }
        
        TwitchEvents.SendTopics(AccessToken);
    }

    public async Task RegisterForEventsAsync(IEnumerable<TwitchChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(channels, nameof(channels));

        var channelsArr = channels as TwitchChannel[] ?? channels.ToArray();
        var tasks = channelsArr.Select(channel =>
        {
            return Task.Run(() =>
            {
                _logger.LogDebug(
                    "Registering for events for {channelName} with broadcaster id {channelBroadcasterId} ...",
                    channel.Name, channel.BroadcasterId);

                channel.PropertyChanged += OnTwitchChannelUpdated;

                TwitchEvents.OnStreamUp += channel.OnStreamUp;
                TwitchEvents.OnStreamDown += channel.OnStreamDown;
                TwitchEvents.OnViewCount += channel.OnViewCount;
            });
        });

        await Task.WhenAll(tasks).ContinueWith(_ =>
        {
            foreach (var c in channelsArr)
                TwitchEvents.ListenToVideoPlayback(c.Id);

            TwitchEvents.SendTopics(AccessToken);
        });
    }

    public async Task LoadChannelDataAsync(TwitchChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel, nameof(channel));
        
        _logger.LogDebug("Loading channel data for {channelName} with broadcaster id {channelBroadcasterId} ...",
            channel.Name, channel.BroadcasterId);

        await Task.Run(() => channel.UpdateChannelData(this));
    }

    public async Task LoadChannelDataAsync(List<TwitchChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(channels, nameof(channels));
        
        var tasks = channels.Select(channel =>
        {
            return Task.Run(() =>
            {
                _logger.LogDebug("Loading channel data for {channelName} with broadcaster id {channelBroadcasterId} ...",
                    channel.Name, channel.BroadcasterId);

                channel.UpdateChannelData(this);
            });
        });
        
        await Task.WhenAll(tasks);
    }

    public void UnregisterFromEvents(TwitchChannel channel)
    {
        _logger.LogDebug("Unregistering from events for {channelName} with broadcaster id {channelBroadcasterId} ...",
            channel.Name, channel.BroadcasterId);

        channel.PropertyChanged -= OnTwitchChannelUpdated;

        TwitchEvents.OnStreamUp -= channel.OnStreamUp;
        TwitchEvents.OnStreamDown -= channel.OnStreamDown;
        TwitchEvents.OnViewCount -= channel.OnViewCount;

        TwitchEvents.ListenToVideoPlayback(channel.Id);

        TwitchEvents.SendTopics(AccessToken, true);
    }

    public bool CanStartRaidCommand(TwitchChannel? channel)
    {
        if (channel == null)
            return false;
        
        return IsRaidStarted == false && UserChannel?.IsLive == true && UserChannel.BroadcasterId != channel.BroadcasterId && channel.IsLive;
    }

    public void StartRaid(TwitchChannel to)
    {
        ArgumentNullException.ThrowIfNull(to);
        
        if (UserChannel?.BroadcasterId == null)
            return;
        
        if (to.BroadcasterId == UserChannel.BroadcasterId)
            return;
        
        RaidedChannel = to;
        RaidTimeProgress = 0;
        IsRaidStarted = true;
        
        var raid = TwitchApi.Helix.Raids.StartRaidAsync(UserChannel.BroadcasterId, to.BroadcasterId).Result.Data.FirstOrDefault();

        if (raid == null)
        {
            _logger.LogError("Could not start raid.");
            return;
        }
        
        _raidStartTime = TimeZoneInfo.ConvertTime(raid.CreatedAt, TimeZoneInfo.Local);
        
        to.LastRaided = DateTime.Now;

        Task.Run(async () =>
        {
            while (IsRaidStarted)
            {
                RaidTimeProgress = Math.Clamp((DateTime.Now - _raidStartTime).TotalSeconds / Constants.RaidDuration, 0, 1);
                await Task.Delay(100);
            }
        });
    }

    public void StartRaidCommand(TwitchChannel? channel)
    {
        if (channel == null)
            return;
        
        StartRaid(channel);
    }

    public void StopRaid()
    {
        if (UserChannel?.BroadcasterId == null)
            return;

        if (IsRaidStarted == false)
            return;

        RaidedChannel = null;
        RaidTimeProgress = 0;
        IsRaidStarted = false;

        TwitchApi.Helix.Raids.CancelRaidAsync(UserChannel.BroadcasterId);
    }

    public void StopRaidCommand()
    {
        StopRaid();
    }

    public void OpenChannelCommand(TwitchChannel? channel)
    {
        if (channel == null)
            return;
        
        var url = $"https://twitch.tv/{channel.Name}";

        _webTools.OpenUrl(url);
    }

    private void OnPubSubServiceClosed(object? sender, EventArgs e)
    {
        _logger.LogWarning("PubSub: Connection closed.");
    }

    private void OnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs e)
    {
        _logger.LogError(e.Exception, "PubSub: {exception}", e.Exception);
    }

    private void OnPubSubLog(object? sender, OnLogArgs e)
    {
        _logger.LogDebug("PubSub: {data}", e.Data);
    }

    private void OnPubSubServiceConnected(object? sender, EventArgs e)
    {
        TwitchEvents.SendTopics(AccessToken);
        _logger.LogInformation("PubSub: Connected.");
    }

    // TODO Not called while raid is ongoing
    private void OnUserRaidUpdate(object? sender, OnRaidUpdateV2Args e)
    {
        RaidParticipants = e.ViewerCount;
    }

    private void OnViewCount(object? sender, OnViewCountArgs e)
    {
        if (UserChannel == null)
            return;

        if (e.ChannelId != UserChannel.Id)
            return;

        UserChannel.OnViewCount(sender, e);
    }

    private void OnUserRaidGo(object? sender, OnRaidGoArgs e)
    {
        if (e.ChannelId != UserChannel?.Id)
            return;

        _logger.LogInformation("Raid started.");

        IsRaidStarted = false;
    }

    private void OnUserStreamDown(object? sender, OnStreamDownArgs e)
    {
        if (UserChannel == null)
            return;

        if (e.ChannelId != UserChannel?.Id)
            return;

        _logger.LogInformation("Stream down.");

        IsRaidStarted = false;

        UserChannel.IsLive = false;
    }

    private void OnUserStreamUp(object? sender, OnStreamUpArgs e)
    {
        if (UserChannel == null)
            return;

        if (e.ChannelId != UserChannel?.Id)
            return;

        _logger.LogInformation("Stream up.");

        IsRaidStarted = false;
        UserChannel.IsLive = true;
    }

    private void OnTwitchChannelUpdated(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TwitchChannel channel)
            return;

        // Only update when should affect the sorting or available items in the list
        //switch (e.PropertyName)
        //{
        //    case nameof(TwitchChannel.IsLive):
        //    case nameof(TwitchChannel.ViewerCount):
        //        TwitchChannelUpdated?.Invoke(this, channel);
        //        break;
        //    
        //    default:
        //        return;
        //}
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    private void OnUserLoginChanged()
    {
        UserLoginChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        OnPropertyChanging(propertyName);
        field = value;
        OnPropertyChanged(propertyName);

        return true;
    }
}