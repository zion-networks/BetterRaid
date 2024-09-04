using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BetterRaid.Misc;
using BetterRaid.Models;
using TwitchLib.Api;

namespace BetterRaid.Services.Implementations;

public class TwitchDataService : ITwitchDataService, INotifyPropertyChanged, INotifyPropertyChanging
{
    private bool _isRaidStarted;
    private TwitchChannel? _userChannel;

    public string AccessToken { get; set; } = string.Empty;
    
    public bool IsRaidStarted
    {
        get => _isRaidStarted;
        set => SetField(ref _isRaidStarted, value);
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
    
    public TwitchDataService()
    {
        TwitchApi = new TwitchAPI();

        if (TryLoadAccessToken(out var token))
        {
            Console.WriteLine($"[INFO][{nameof(TwitchDataService)}] Found access token.");
            Task.Run(() => ConnectApiAsync(Constants.TwitchClientId, token));
        }
        else
        {
            Console.WriteLine($"[INFO][{nameof(TwitchDataService)}] No access token found.");
        }
    }
    
    public async Task ConnectApiAsync(string clientId, string accessToken)
    {
        Console.WriteLine($"[INFO][{nameof(TwitchDataService)}] Connecting to Twitch API ...");
        
        AccessToken = accessToken;
        
        TwitchApi.Settings.ClientId = clientId;
        TwitchApi.Settings.AccessToken = accessToken;

        if (TryGetUserChannel(out var channel))
        {
            UserChannel = channel;
            Console.WriteLine($"[INFO][{nameof(TwitchDataService)}] Connected to Twitch API as {channel?.Name}.");
        }
        else
        {
            UserChannel = null;
            
            Console.WriteLine($"[ERROR][{nameof(TwitchDataService)}] Could not get user channel.");
            Console.WriteLine($"[ERROR][{nameof(TwitchDataService)}] Failed to connect to Twitch API.");
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
    
    public bool TryGetUserChannel(out TwitchChannel? userChannel)
    {
        userChannel = null;
        
        try
        {
            var user = TwitchApi.Helix.Users.GetUsersAsync().Result.Users[0];
            userChannel = new TwitchChannel(user.Login);

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERROR][{nameof(TwitchDataService)}] {e.Message}");
            return false;
        }
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

    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        OnPropertyChanging(propertyName);
        field = value;
        OnPropertyChanged(propertyName);
        
        return true;
    }

}