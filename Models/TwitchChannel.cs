using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace BetterRaid.Models;

public class TwitchChannel : INotifyPropertyChanged
{
    private string? viewerCount;
    private bool isLive;
    private string? name;
    private string? displayName;
    private string? thumbnailUrl;

    public string? BroadcasterId
    {
        get;
        set;
    }
    public string? Name
    {
        get => name;
        set
        {
            if (value == name)
                return;

            name = value;
            OnPropertyChanged();
        }
    }
    public bool IsLive
    {
        get => isLive;
        set
        {
            if (value == isLive)
                return;

            isLive = value;
            OnPropertyChanged();
        }
    }
    public string? ViewerCount
    {
        get => viewerCount;
        set
        {
            if (value == viewerCount)
                return;

            viewerCount = value;
            OnPropertyChanged();
        }
    }

    public string? ThumbnailUrl
    {
        get => thumbnailUrl;
        set
        {
            if (value == thumbnailUrl)
                return;

            thumbnailUrl = value;
            OnPropertyChanged();
        }
    }

    public string? DisplayName
    {
        get => displayName;
        set
        {
            if (value == displayName)
                return;
            
            displayName = value;
            OnPropertyChanged();
        }
    }

    public TwitchChannel(string channelName)
    {
        Name = channelName;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}