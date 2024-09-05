using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using BetterRaid.Services;
using TwitchLib.PubSub.Events;

namespace BetterRaid.Models;

public class TwitchChannel : INotifyPropertyChanged
{
    private string? _broadcasterId;
    private string? _viewerCount;
    private bool _isLive;
    private string? _name;
    private string? _displayName;
    private string? _thumbnailUrl;
    private string? _category;
    private string? _title;
    private DateTime? _lastRaided;
    private string? _id;

    public string? Id
    {
        get => _id;
        set
        {
            if (value == _id)
                return;
            
            _id = value;
            OnPropertyChanged();
        }
    }

    public string? BroadcasterId
    {
        get => _broadcasterId;
        set
        {
            if (value == _broadcasterId)
                return;

            _broadcasterId = value;
            OnPropertyChanged();
        }
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

    public void UpdateChannelData(ITwitchService service)
    {
        var channel = service.TwitchApi.Helix.Search.SearchChannelsAsync(Name).Result.Channels
            .FirstOrDefault(c => c.BroadcasterLogin.Equals(Name, StringComparison.CurrentCultureIgnoreCase));

        if (channel == null)
            return;
        
        var stream = service.TwitchApi.Helix.Streams.GetStreamsAsync(userLogins: [ Name ]).Result.Streams
            .FirstOrDefault(s => s.UserLogin.Equals(Name, StringComparison.CurrentCultureIgnoreCase));

        Id = channel.Id;
        BroadcasterId = channel.Id;
        DisplayName = channel.DisplayName;
        ThumbnailUrl = channel.ThumbnailUrl;
        Category = channel.GameName;
        Title = channel.Title;
        IsLive = channel.IsLive;
        ViewerCount = stream?.ViewerCount == null
            ? null
            : $"{stream.ViewerCount}";
    }

    public void OnStreamUp(object? sender, OnStreamUpArgs args)
    {
        if (args.ChannelId != BroadcasterId)
            return;
        
        IsLive = true;
    }

    public void OnStreamDown(object? sender, OnStreamDownArgs args)
    {
        if (args.ChannelId != BroadcasterId)
            return;

        IsLive = false;
    }

    public void OnViewCount(object? sender, OnViewCountArgs args)
    {
        if (args.ChannelId != BroadcasterId)
            return;

        ViewerCount = $"{args.Viewers}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}