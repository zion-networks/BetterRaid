using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using BetterRaid.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitchLib.PubSub.Events;

namespace BetterRaid.Models;

[JsonObject]
public class TwitchChannel : INotifyPropertyChanged, IEqualityComparer<TwitchChannel>
{
    private string? _id;
    private string _name;
    private string? _broadcasterId;
    private int _viewerCount;
    private bool _isLive;
    private string? _displayName;
    private string? _thumbnailUrl;
    private string? _category;
    private string? _title;
    private DateTime? _lastRaided;
    
    private ILogger<TwitchChannel>? Logger { get; set; }

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
    
    public string Name
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
    
    [JsonIgnore]
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
    
    [JsonIgnore]
    public int ViewerCount
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

    [JsonIgnore]
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
    
    [JsonIgnore]
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

    public TwitchChannel(string? channelName, ILogger<TwitchChannel>? logger = null)
    {
        _name = channelName ?? string.Empty;

        Logger = logger;
    }

    public void UpdateChannelData(ITwitchService service)
    {
        var channel = service.TwitchApi.Helix.Search.SearchChannelsAsync(Name).Result.Channels
            .FirstOrDefault(c => c.BroadcasterLogin.Equals(Name, StringComparison.CurrentCultureIgnoreCase));

        if (channel == null)
        {
            Logger?.LogError("Channel {ChannelName} not found", Name);
            return;
        }
        
        var stream = service.TwitchApi.Helix.Streams.GetStreamsAsync(userLogins: [ Name ]).Result.Streams
            .FirstOrDefault(s => s.UserLogin.Equals(Name, StringComparison.CurrentCultureIgnoreCase));

        Id = channel.Id;
        BroadcasterId = channel.Id;
        DisplayName = channel.DisplayName;
        ThumbnailUrl = channel.ThumbnailUrl;
        Category = channel.GameName;
        Title = channel.Title;
        IsLive = channel.IsLive;
        ViewerCount = stream?.ViewerCount ?? 0;
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

        ViewerCount = args.Viewers;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool Equals(TwitchChannel? x, TwitchChannel? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        
        if (x is null)
            return false;
        
        if (y is null)
            return false;
        
        if (x.GetType() != y.GetType())
            return false;
        
        return x._id == y._id;
    }

    public int GetHashCode(TwitchChannel obj)
    {
        return (obj._id != null ? obj._id.GetHashCode() : 0);
    }
}