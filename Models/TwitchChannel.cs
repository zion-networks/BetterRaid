using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace BetterRaid.Models;

public class TwitchChannel : INotifyPropertyChanged
{
    private string? _viewerCount;
    private bool _isLive;
    private string? _name;
    private string? _displayName;
    private string? _thumbnailUrl;
    private string? _category;
    private string? _title;
    private DateTime? _lastRaided;

    public string? BroadcasterId
    {
        get;
        set;
    }
    public string? Name
    {
        get => _name;
        set
        {
            if (value == _name)
                return;

            _name = value;
            OnPropertyChanged();
        }
    }
    public bool IsLive
    {
        get => _isLive;
        set
        {
            if (value == _isLive)
                return;

            _isLive = value;
            OnPropertyChanged();
        }
    }
    public string? ViewerCount
    {
        get => _viewerCount;
        set
        {
            if (value == _viewerCount)
                return;

            _viewerCount = value;
            OnPropertyChanged();
        }
    }

    public string? ThumbnailUrl
    {
        get => _thumbnailUrl;
        set
        {
            if (value == _thumbnailUrl)
                return;

            _thumbnailUrl = value;
            OnPropertyChanged();
        }
    }

    public string? DisplayName
    {
        get => _displayName;
        set
        {
            if (value == _displayName)
                return;
            
            _displayName = value;
            OnPropertyChanged();
        }
    }

    public string? Category
    {
        get => _category;
        set
        {
            if (value == _category)
                return;
            
            _category = value;
            OnPropertyChanged();
        }
    }
    
    public string? Title
    {
        get => _title;
        set
        {
            if (value == _title)
                return;
            
            _title = value;
            OnPropertyChanged();
        }
    }
    
    public DateTime? LastRaided
    {
        get => _lastRaided;
        set
        {
            if (value == _lastRaided)
                return;
            
            _lastRaided = value;
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