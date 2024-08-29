using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using BetterRaid.Events;
using BetterRaid.Models;
using TwitchLib.Api.Helix.Models.Raids.StartRaid;
using TwitchLib.Api.Helix.Models.Search;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;

namespace BetterRaid.ViewModels;

public class RaidButtonViewModel : ViewModelBase
{
    private TwitchChannel? _channel;
    private SolidColorBrush _viewerCountColor = new SolidColorBrush(Color.FromRgb(byte.MaxValue, byte.MaxValue, byte.MaxValue));
    private bool _hideDeleteButton;
    private bool _isAd;

    public string ChannelName
    {
        get;
        set;
    }

    public bool HideDeleteButton
    {
        get => _hideDeleteButton;
        set => SetProperty(ref _hideDeleteButton, value);
    }

    public bool IsAd
    {
        get => _isAd;
        set => SetProperty(ref _isAd, value);
    }

    public TwitchChannel? Channel => _channel ?? new TwitchChannel(ChannelName);

    public SolidColorBrush ViewerCountColor
    {
        get => _viewerCountColor;
        set => SetProperty(ref _viewerCountColor, value);
    }

    public MainWindowViewModel? MainVm { get; set; }

    public DateTime? LastRaided => MainVm?.Database?.GetLastRaided(ChannelName);

    public event EventHandler<ChannelDataChangedEventArgs>? ChannelDataChanged;

    public RaidButtonViewModel(string channelName)
    {
        ChannelName = channelName;
    }

    public async Task<bool> GetOrUpdateChannelAsync()
    {
        Console.WriteLine("[DEBUG] Updating channel '{0}' ...", ChannelName);

        var currentChannelData = await GetChannelAsync(ChannelName);

        if (currentChannelData == null)
            return false;

        var currentStreamData = await GetStreamAsync(currentChannelData);

        var swapChannel = new TwitchChannel(ChannelName)
        {
            BroadcasterId = currentChannelData.Id,
            Name = ChannelName,
            DisplayName = currentChannelData.DisplayName,
            IsLive = currentChannelData.IsLive,
            ThumbnailUrl = currentChannelData.ThumbnailUrl,
            ViewerCount = currentStreamData?.ViewerCount == null
                             ? "(Offline)"
                             : $"{currentStreamData?.ViewerCount} Viewers",
            Category = currentStreamData?.GameName
        };

        if (_channel != null)
        {
            _channel.PropertyChanged -= OnChannelDataChanged;
        }

        Dispatcher.UIThread.Invoke(() => {
            ViewerCountColor = new SolidColorBrush(Color.FromRgb(
                r: swapChannel.IsLive ? (byte) 0      : byte.MaxValue,
                g: swapChannel.IsLive ? byte.MaxValue : (byte) 0,
                b: 0)
            );

            _channel = swapChannel;
            OnPropertyChanged(nameof(Channel));
        });

        if (_channel != null)
        {
            _channel.PropertyChanged += OnChannelDataChanged;
        }

        Console.WriteLine("[DEBUG] DONE Updating channel '{0}'", ChannelName);

        return true;
    }

    private async Task<Channel?> GetChannelAsync(string channelName)
    {
        if (App.TwitchApi == null)
            return null;

        if (string.IsNullOrEmpty(channelName))
            return null;

        var channels = await App.TwitchApi.Helix.Search.SearchChannelsAsync(channelName);
        var exactChannel = channels.Channels.FirstOrDefault(c => c.BroadcasterLogin.Equals(channelName, StringComparison.CurrentCultureIgnoreCase));

        return exactChannel;
    }

    private async Task<Stream?> GetStreamAsync(Channel currentChannelData)
    {
        if (App.TwitchApi == null)
            return null;

        if (currentChannelData == null)
            return null;

        var streams = await App.TwitchApi.Helix.Streams.GetStreamsAsync(userLogins: [currentChannelData.BroadcasterLogin]);
        var exactStream = streams.Streams.FirstOrDefault(s => s.UserLogin == currentChannelData.BroadcasterLogin);

        return exactStream;
    }

    public async Task RaidChannel()
    {
        if (App.TwitchApi == null)
            return;
        
        if (Channel == null)
            return;
        
        if (string.IsNullOrWhiteSpace(App.TwitchBroadcasterId))
            return;
        
        if (App.TwitchBroadcasterId == Channel.BroadcasterId)
            return;

        try
        {
            await App.TwitchApi.Helix.Raids.StartRaidAsync(App.TwitchBroadcasterId, Channel.BroadcasterId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);

            return;
        }

        if (MainVm?.Database != null)
        {
            MainVm.Database.SetRaided(ChannelName, DateTime.Now);
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
        switch (e.PropertyName)
        {
            case "IsLive":
                OnChannelDataChanged(ChannelDataChangedEventArgs.FromIsLive(false, true));
                break;
            
            case "ViewerCount":
                OnChannelDataChanged(ChannelDataChangedEventArgs.FromViewerCount(0, 10));
                break;
        }
    }

    private void OnChannelDataChanged(ChannelDataChangedEventArgs args)
    {
        ChannelDataChanged?.Invoke(this, args);
    }
}