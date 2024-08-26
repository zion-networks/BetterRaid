using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using BetterRaid.Models;
using TwitchLib.Api.Helix.Models.Raids.StartRaid;
using TwitchLib.Api.Helix.Models.Search;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

namespace BetterRaid.ViewModels;

public class RaidButtonViewModel : ViewModelBase
{
    private TwitchChannel? _channel;
    private SolidColorBrush _viewerCountColor = new SolidColorBrush(Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue));

    public required string ChannelName
    {
        get;
        set;
    }

    public TwitchChannel Channel => _channel ?? new TwitchChannel(ChannelName);

    public SolidColorBrush ViewerCountColor
    {
        get => _viewerCountColor;
        set => SetProperty(ref _viewerCountColor, value);
    }

    public MainWindowViewModel? MainVm { get; set; }

    public async Task<bool> GetOrUpdateChannelAsync()
    {
        if (_channel == null)
        {
            _channel = new TwitchChannel(ChannelName);
            _channel.PropertyChanged += OnChannelDataChanged;
        }

        var currentChannelData = await GetChannelAsync(ChannelName);

        if (currentChannelData == null)
            return false;

        var currentStreamData = await GetStreamAsync(currentChannelData);

        _channel.BroadcasterId = currentChannelData.Id;
        _channel.Name = ChannelName;
        _channel.DisplayName = currentChannelData.DisplayName;
        _channel.IsLive = currentChannelData.IsLive;
        _channel.ThumbnailUrl = currentChannelData.ThumbnailUrl;
        _channel.ViewerCount = currentStreamData?.ViewerCount == null
                             ? "(Offline)"
                             : $"{currentStreamData?.ViewerCount} Viewers";

        if (_channel.IsLive)
        {
            ViewerCountColor = new SolidColorBrush(Color.FromRgb(0, byte.MaxValue, 0));
        }
        else
        {
            ViewerCountColor = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 0, 0));
        }

        return true;
    }

    private async Task<Channel?> GetChannelAsync(string channelName)
    {
        if (App.TwitchAPI == null)
            return null;

        if (string.IsNullOrEmpty(channelName))
            return null;

        var channels = await App.TwitchAPI.Helix.Search.SearchChannelsAsync(channelName);
        var exactChannel = channels.Channels.FirstOrDefault(c => c.BroadcasterLogin.Equals(channelName, StringComparison.CurrentCultureIgnoreCase));

        return exactChannel;
    }

    private async Task<Stream?> GetStreamAsync(Channel currentChannelData)
    {
        if (App.TwitchAPI == null)
            return null;

        if (currentChannelData == null)
            return null;

        var streams = await App.TwitchAPI.Helix.Streams.GetStreamsAsync(userLogins: [currentChannelData.BroadcasterLogin]);
        var exactStream = streams.Streams.FirstOrDefault(s => s.UserLogin == currentChannelData.BroadcasterLogin);

        return exactStream;
    }

    public async Task RaidChannel()
    {
        if (App.TwitchAPI == null)
            return;
        
        StartRaidResponse? raid = null;

        try
        {
            raid = await App.TwitchAPI.Helix.Raids.StartRaidAsync(App.TwitchBroadcasterId, Channel.BroadcasterId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);

            return;
        }
        
        if (raid.Data.Length > 0)
        {
            var createdAt = raid.Data[0].CreatedAt;
            var isMature = raid.Data[0].IsMature;
        }
    }

    public void RemoveChannel()
    {
        if (MainVm?.Database == null)
            return;
        
        MainVm.Database.RemoveChannel(ChannelName);
    }

    private void OnChannelDataChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Channel));
    }
}