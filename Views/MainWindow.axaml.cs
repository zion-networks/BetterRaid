using System;
using System.Collections.Generic;
using System.Linq;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BetterRaid.Models;
using BetterRaid.ViewModels;
using TwitchLib.Client.Events;

namespace BetterRaid.Views;

public partial class MainWindow : Window
{
    private string[] _channelNames = [
        "Cedricun", // Ehrenbruder
        "ZanTal",   // Ehrenschwester
        "PropzMaster",
        "Artimus83",
        "HyperonsLive",
        "theshroomlife",
        "Robocraft999",
        "sllikson",
        "Aron_dc",
        "AIEsports"
    ];

    public MainWindow()
    {
        InitializeComponent();
        PrepareRaidGrid();
        ConnectToTwitch();
    }

    private void PrepareRaidGrid()
    {
        var rows = (int)Math.Ceiling(_channelNames.Length / 3.0);

        for (var i = 0; i < rows; i++)
        {
            raidGrid.RowDefinitions.Add(new RowDefinition(GridLength.Parse("200")));
        }

        var colIndex = 0;
        var rowIndex = 0;
        foreach (var channel in _channelNames)
        {
            var btn = new Button
            {
                Content = channel,
                DataContext = new TwitchChannel(channel),
                Margin = Thickness.Parse("5"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            Grid.SetColumn(btn, colIndex);
            Grid.SetRow(btn, rowIndex);

            raidGrid.Children.Add(btn);

            colIndex++;
            if (colIndex % 3 == 0)
            {
                colIndex = 0;
                rowIndex++;
            }
        }
    }

    private void ConnectToTwitch()
    {
        if (App.TwitchClient != null && App.TwitchAPI != null)
        {
            foreach (var c in raidGrid.Children)
            {
                if (c is Button btn)
                {
                    var channel = (btn.DataContext as TwitchChannel)?.Name;

                    if (string.IsNullOrEmpty(channel) == false)
                    {
                        var channels = App.TwitchAPI.Helix.Search.SearchChannelsAsync(channel).Result;
                        var exactChannel = channels.Channels.FirstOrDefault(c => c.BroadcasterLogin.ToLower() == channel.ToLower());

                        Dispatcher.UIThread.Invoke(() =>
                        {
                            if (exactChannel != null)
                            {
                                if (btn.DataContext is TwitchChannel ctx)
                                {
                                    ctx.BroadcasterId = exactChannel.Id;
                                    var ib = new ImageBrush();
                                    ImageBrushLoader.SetSource(ib, exactChannel.ThumbnailUrl);
                                    btn.Background = ib;
                                    
                                    var streamInfo = App.TwitchAPI.Helix.Streams.GetStreamsAsync(userLogins: new List<string>([channel])).Result;
                                    var exactStreamInfo = streamInfo.Streams.FirstOrDefault(s => s.UserLogin.ToLower() == channel.ToLower());
                                    
                                    if (exactStreamInfo != null)
                                    {
                                        if (exactChannel.IsLive)
                                        {
                                            btn.Foreground = new SolidColorBrush(new Color(byte.MaxValue, 0, byte.MaxValue, 0));
                                            btn.Content = $"{exactChannel.DisplayName} ({exactStreamInfo.ViewerCount})";
                                        }
                                        else
                                        {
                                            btn.Foreground = new SolidColorBrush(new Color(byte.MaxValue, byte.MaxValue, 0, 0));
                                            btn.Content = $"{exactChannel.DisplayName} (Offline)";
                                        }

                                        ctx.ViewerCount = exactStreamInfo.ViewerCount;
                                    }
                                    else
                                    {
                                        btn.Foreground = new SolidColorBrush(new Color(byte.MaxValue, byte.MaxValue, 0, 0));
                                        btn.Content = $"{exactChannel.DisplayName} (Offline)";
                                    }
                                }
                            }
                        });
                    }
                }
            }
        }
    }
}