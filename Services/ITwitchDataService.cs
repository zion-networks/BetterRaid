using System.ComponentModel;
using BetterRaid.Models;
using TwitchLib.Api;

namespace BetterRaid.Services;

public interface ITwitchDataService
{
    public string? AccessToken { get; set; }
    public TwitchChannel? UserChannel { get; set; }
    public TwitchAPI TwitchApi { get; }
    public bool IsRaidStarted { get; set; }

    public void ConnectApi(string clientId, string accessToken);
    public void SaveAccessToken(string token);
    public bool TryGetUserChannel(out TwitchChannel? userChannel);
    public string GetOAuthUrl();
    public void StartRaid(string from, string to);
    public void StartRaidCommand(object? arg);
    public void StopRaid();
    public void StopRaidCommand();
    public void OpenChannelCommand(object? arg);
    
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;
}