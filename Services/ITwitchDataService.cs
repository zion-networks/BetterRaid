using System.ComponentModel;
using System.Threading.Tasks;
using BetterRaid.Models;
using TwitchLib.Api;

namespace BetterRaid.Services;

public interface ITwitchDataService
{
    public string? AccessToken { get; set; }
    public TwitchChannel? UserChannel { get; set; }
    public TwitchAPI TwitchApi { get; }
    public bool IsRaidStarted { get; set; }

    public Task ConnectApiAsync(string clientId, string accessToken);
    public void SaveAccessToken(string token);
    public bool TryGetUserChannel(out TwitchChannel? userChannel);
    public string GetOAuthUrl();
    public void StartRaid(string from, string to);
    public bool CanStartRaidCommand(object? arg);
    public void StartRaidCommand(object? arg);
    public void StopRaid();
    public void StopRaidCommand();
    public void OpenChannelCommand(object? arg);
    
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;
}