using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BetterRaid.ViewModels;
using BetterRaid.Views;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace BetterRaid;

public partial class App : Application
{
    public static int AutoUpdateDelay = 10_000;
    public static string TwitchBroadcasterId = "";
    public static string TwitchChannelName = "";
    public static string TokenClientId = "";
    public static string TokenClientSecret = "";
    public static string TokenClientAccess = "";
    public static TwitchClient? TwitchClient = null;
    public static TwitchAPI? TwitchAPI = null;

    public override void Initialize()
    {
        try
        {
            var tokenFile      = "zn_twitch.secret";
            var profilePath    = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var tokenFilePath  = Path.Combine(profilePath, tokenFile);
            var tokenFileLines = File.ReadAllLines(tokenFilePath);
            TwitchChannelName  = tokenFileLines[0].Split('=')[1];
            TokenClientId      = tokenFileLines[1].Split('=')[1];
            TokenClientSecret  = tokenFileLines[2].Split('=')[1];
            TokenClientAccess  = tokenFileLines[3].Split('=')[1];
        }
        catch (Exception)
        {
            Console.WriteLine("[ERROR] Failed to read token from secret file!");
            Environment.Exit(1);
        }

        var creds = new ConnectionCredentials(TwitchChannelName, TokenClientAccess);
        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };

        var customClient = new WebSocketClient(clientOptions);
        TwitchClient = new TwitchClient(customClient);

        TwitchClient.Initialize(creds, TwitchChannelName);
        TwitchClient.OnConnected += OnConnected;
        TwitchClient.OnConnectionError += OnConnectionError;

        TwitchClient.Connect();

        TwitchAPI = new TwitchAPI();
        TwitchAPI.Settings.ClientId = TokenClientId;
        TwitchAPI.Settings.AccessToken = TokenClientAccess;

        var channels = TwitchAPI.Helix.Search.SearchChannelsAsync(TwitchChannelName).Result;
        var exactChannel = channels.Channels.FirstOrDefault(c => c.BroadcasterLogin == TwitchChannelName);

        if (exactChannel != null)
        {
            TwitchBroadcasterId = exactChannel.Id;
            Console.WriteLine($"TWITCH BROADCASTER ID SET TO {TwitchBroadcasterId}");
        }
        else
        {
            Console.WriteLine("FAILED TO SET BROADCASTER ID!");
        }

        AvaloniaXamlLoader.Load(this);
    }

    private void OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        Console.WriteLine("[ERROR] Twitch Client failed to connect!");
    }

    private void OnConnected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine("[INFO] Twitch Client connected!");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}