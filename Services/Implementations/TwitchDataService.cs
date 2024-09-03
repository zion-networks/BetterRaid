using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BetterRaid.Misc;

namespace BetterRaid.Services.Implementations;

public class TwitchDataService : ITwitchDataService, INotifyPropertyChanged
{
    private bool _isRaidStarted;

    public bool IsRaidStarted
    {
        get => _isRaidStarted;
        set => SetField(ref _isRaidStarted, value);
    }
    
    public void StartRaid(string from, string to)
    {
        // TODO: Also check, if the logged in user is live
        
        App.TwitchApi?.Helix.Raids.StartRaidAsync(from, to);
        IsRaidStarted = true;
    }

    public void StartRaidCommand(object? arg)
    {
        if (arg == null || App.TwitchBroadcasterId == null)
        {
            return;
        }
        
        var from = App.TwitchBroadcasterId;
        var to = arg.ToString()!;
        
        StartRaid(from, to);
    }

    public void StopRaid()
    {
        if (IsRaidStarted == false)
            return;
        
        App.TwitchApi?.Helix.Raids.CancelRaidAsync(App.TwitchBroadcasterId);
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}