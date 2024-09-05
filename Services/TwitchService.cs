using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BetterRaid.Misc;
using BetterRaid.Models;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using OnEmoteOnlyArgs = TwitchLib.PubSub.Events.OnEmoteOnlyArgs;
using OnLogArgs = TwitchLib.PubSub.Events.OnLogArgs;

namespace BetterRaid.Services;

public interface ITwitchService
{
    public string? AccessToken { get; }
    public TwitchChannel? UserChannel { get; set; }
    public TwitchAPI TwitchApi { get; }
    public bool IsRaidStarted { get; set; }

    public Task ConnectApiAsync(string clientId, string accessToken);
    public string GetOAuthUrl();
    public void StartRaid(string from, string to);
    public bool CanStartRaidCommand(object? arg);
    public void StartRaidCommand(object? arg);
    public void StopRaid();
    public void StopRaidCommand();
    public void OpenChannelCommand(object? arg);
    public void RegisterForEvents(TwitchChannel channel);
    public void UnregisterFromEvents(TwitchChannel channel);
    
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class TwitchService : ITwitchService, INotifyPropertyChanged, INotifyPropertyChanging
{
    private bool _isRaidStarted;
    private TwitchChannel? _userChannel;
    private readonly List<TwitchChannel> _registeredChannels;
    private User? _user;

    public string AccessToken { get; private set; } = string.Empty;
    
    public bool IsRaidStarted
    {
        get => _isRaidStarted;
        set => SetField(ref _isRaidStarted, value);
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
            if (_userChannel != null && _userChannel.Name?.Equals(value?.Name) == true)
                return;
            
            SetField(ref _userChannel, value);

            _userChannel?.UpdateChannelData(this);
        }
    }

    public TwitchAPI TwitchApi { get; }
    public TwitchPubSub TwitchEvents { get;  }
    
    public TwitchService()
    {
        _registeredChannels = [];
        
        TwitchApi = new TwitchAPI();
        TwitchEvents = new TwitchPubSub();
        
        if (TryLoadAccessToken(out var token))
        {
            Console.WriteLine($"[INFO][{nameof(TwitchService)}] Found access token.");
            Task.Run(() => ConnectApiAsync(Constants.TwitchClientId, token))
                .ContinueWith(_ => ConnectTwitchEvents(token));
        }
        else
        {
            Console.WriteLine($"[INFO][{nameof(TwitchService)}] No access token found.");
        }
    }

    private async Task ConnectTwitchEvents(string token)
    {
        if (UserChannel == null || User == null)
            return;

        Console.WriteLine($"[INFO][{nameof(TwitchService)}] Connecting to Twitch Events ...");

        TwitchEvents.OnRaidGo += OnUserRaidGo;
        TwitchEvents.OnRaidUpdateV2 += OnUserRaidUpdate;
        TwitchEvents.OnStreamUp += OnUserStreamUp;
        TwitchEvents.OnStreamDown += OnUserStreamDown;
        
        TwitchEvents.ListenToRaid(UserChannel.BroadcasterId);
        TwitchEvents.ListenToVideoPlayback(UserChannel.BroadcasterId);
        
        TwitchEvents.SendTopics(token);
        TwitchEvents.Connect();
        
        RegisterForEvents(UserChannel);
        
        Console.WriteLine($"[INFO][{nameof(TwitchService)}] Connected to Twitch Events.");
        
        await Task.CompletedTask;
    }

    public async Task ConnectApiAsync(string clientId, string accessToken)
    {
        Console.WriteLine($"[INFO][{nameof(TwitchService)}] Connecting to Twitch API ...");
        
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
            
            Console.WriteLine($"[ERROR][{nameof(TwitchService)}] Could not get user.");
        }

        if (TryGetUserChannel(out var channel))
        {
            UserChannel = channel;
            Console.WriteLine($"[INFO][{nameof(TwitchService)}] Connected to Twitch API as {channel?.Name}.");
        }
        else
        {
            UserChannel = null;
            
            Console.WriteLine($"[ERROR][{nameof(TwitchService)}] Could not get user channel.");
        }
        
        if (User == null || UserChannel == null)
        {
            Console.WriteLine($"[ERROR][{nameof(TwitchService)}] Could not connect to Twitch API.");
        }
        
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
    
    public void SaveAccessToken(string token)
    {
        File.WriteAllText(Constants.TwitchOAuthAccessTokenFilePath, token);
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
            Console.WriteLine($"[ERROR][{nameof(TwitchService)}] {e.Message}");
            return false;
        }
    }

    public bool TryGetUserChannel(out TwitchChannel? channel)
    {
        channel = null;
        
        if (User == null)
            return false;
        
        channel = new TwitchChannel(User.Login);
        
        return true;
    }

    public void RegisterForEvents(TwitchChannel channel)
    {
        TwitchEvents.OnStreamUp += channel.OnStreamUp;
        TwitchEvents.OnStreamDown += channel.OnStreamDown;
        TwitchEvents.OnViewCount += channel.OnViewCount;
        
        TwitchEvents.ListenToVideoPlayback(channel.BroadcasterId);
        
        TwitchEvents.SendTopics(AccessToken);
        
        _registeredChannels.Add(channel);
    }
    
    public void UnregisterFromEvents(TwitchChannel channel)
    {
        TwitchEvents.OnStreamUp -= channel.OnStreamUp;
        TwitchEvents.OnStreamDown -= channel.OnStreamDown;
        TwitchEvents.OnViewCount -= channel.OnViewCount;
        
        TwitchEvents.ListenToVideoPlayback(channel.BroadcasterId);
        
        TwitchEvents.SendTopics(AccessToken, true);
        
        _registeredChannels.Remove(channel);
    }

    public string GetOAuthUrl()
    {
        var scopes = string.Join("+", Constants.TwitchOAuthScopes);
        
        return $"https://id.twitch.tv/oauth2/authorize"
               + $"?client_id={Constants.TwitchClientId}"
               + $"&redirect_uri={Constants.TwitchOAuthRedirectUrl}"
               + $"&response_type={Constants.TwitchOAuthResponseType}"
               + $"&scope={scopes}";
    }
    
    public void StartRaid(string from, string to)
    {
        // TODO: Also check, if the logged in user is live
        
        TwitchApi.Helix.Raids.StartRaidAsync(from, to);
        IsRaidStarted = true;
    }

    public bool CanStartRaidCommand(object? arg)
    {
        return UserChannel?.IsLive == true && IsRaidStarted == false;
    }

    public void StartRaidCommand(object? arg)
    {
        if (arg == null || UserChannel?.BroadcasterId == null)
        {
            return;
        }
        
        var from = UserChannel.BroadcasterId!;
        var to = arg.ToString()!;
        
        StartRaid(from, to);
    }

    public void StopRaid()
    {
        if (UserChannel?.BroadcasterId == null)
            return;
        
        if (IsRaidStarted == false)
            return;
        
        TwitchApi.Helix.Raids.CancelRaidAsync(UserChannel.BroadcasterId);
        IsRaidStarted = false;
    }

    public void StopRaidCommand()
    {
        StopRaid();
    }

    public void OpenChannelCommand(object? arg)
    {
        var channelName = arg?.ToString();
        if (string.IsNullOrEmpty(channelName))
            return;
        
        var url = $"https://twitch.tv/{channelName}";
        
        Tools.OpenUrl(url);
    }

    private void OnUserRaidUpdate(object? sender, OnRaidUpdateV2Args e)
    {
        
    }

    private void OnUserRaidGo(object? sender, OnRaidGoArgs e)
    {
        IsRaidStarted = false;
    }

    private void OnUserStreamDown(object? sender, OnStreamDownArgs e)
    {
        IsRaidStarted = false;
    }

    private void OnUserStreamUp(object? sender, OnStreamUpArgs e)
    {
        IsRaidStarted = false;
    }

    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
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