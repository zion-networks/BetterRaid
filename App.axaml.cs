using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BetterRaid.Services;
using BetterRaid.ViewModels;
using BetterRaid.Views;
using Microsoft.Extensions.DependencyInjection;
using TwitchLib.Api;

namespace BetterRaid;

public partial class App : Application
{
    private readonly ServiceCollection _services = [];
    private ServiceProvider? _provider;
    
    internal static TwitchAPI? TwitchApi = null;
    internal static int AutoUpdateDelay = 10_000;
    internal static bool HasUserZnSubbed = false;
    internal static string BetterRaidDataPath = "";
    internal static string TwitchBroadcasterId = "";
    internal static string TwitchOAuthAccessToken = "";
    internal static string TwitchOAuthAccessTokenFilePath = "";
    internal static string TokenClientId = "kkxu4jorjrrc5jch1ito5i61hbev2o";
    internal static readonly string TwitchOAuthRedirectUrl = "http://localhost:9900";
    internal static readonly string TwitchOAuthResponseType = "token";
    internal static readonly string[] TwitchOAuthScopes = [
        "channel:manage:raids",
        "user:read:subscriptions"
    ];
    internal static readonly string TwitchOAuthUrl = $"https://id.twitch.tv/oauth2/authorize"
                                                    + $"?client_id={TokenClientId}"
                                                    + "&redirect_uri=http://localhost:9900"
                                                    + $"&response_type={TwitchOAuthResponseType}"
                                                    + $"&scope={string.Join("+", TwitchOAuthScopes)}";

    public const string ChannelPlaceholderImageUrl = "https://cdn.pixabay.com/photo/2018/11/13/22/01/avatar-3814081_1280.png";

    public IServiceProvider? Provider => _provider;
    
    public override void Initialize()
    {
        InitializeServices();
        
        var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
                BetterRaidDataPath = Path.Combine(userHomeDir, "AppData", "Roaming", "BetterRaid");
                break;
            case PlatformID.Unix:
                BetterRaidDataPath = Path.Combine(userHomeDir, ".config", "BetterRaid");
                break;
            case PlatformID.MacOSX:
                BetterRaidDataPath = Path.Combine(userHomeDir, "Library", "Application Support", "BetterRaid");
                break;
        }

        if (!Directory.Exists(BetterRaidDataPath))
            Directory.CreateDirectory(BetterRaidDataPath);

        TwitchOAuthAccessTokenFilePath = Path.Combine(BetterRaidDataPath, ".access_token");

        if (File.Exists(TwitchOAuthAccessTokenFilePath))
        {
            TwitchOAuthAccessToken = File.ReadAllText(TwitchOAuthAccessTokenFilePath);
            InitTwitchClient();
        }

        AvaloniaXamlLoader.Load(_provider, this);
    }

    private void InitializeServices()
    {
        _services.AddSingleton<ITwitchDataService, TwitchDataService>();
        _services.AddTransient<MainWindowViewModel>();
        _services.AddTransient<AboutWindowViewModel>();
        
        _provider = _services.BuildServiceProvider();
    }

    public static void InitTwitchClient(bool overrideToken = false)
    {
        Console.WriteLine("[INFO] Initializing Twitch Client...");

        TwitchApi = new TwitchAPI();
        TwitchApi.Settings.ClientId = TokenClientId;
        TwitchApi.Settings.AccessToken = TwitchOAuthAccessToken;

        Console.WriteLine("[INFO] Testing Twitch API connection...");

        var user = TwitchApi.Helix.Users.GetUsersAsync().Result.Users.FirstOrDefault();
        if (user == null)
        {
            TwitchApi = null;
            Console.WriteLine("[ERROR] Failed to connect to Twitch API!");
            return;
        }

        var channel = TwitchApi.Helix.Search
                        .SearchChannelsAsync(user.Login).Result.Channels
                        .FirstOrDefault(c => c.BroadcasterLogin == user.Login);
        
        var userSubs = TwitchApi.Helix.Subscriptions.CheckUserSubscriptionAsync(
            userId: user.Id,
            broadcasterId: "1120558409"
        ).Result.Data;

        if (userSubs.Length > 0 && userSubs.Any(s => s.BroadcasterId == "1120558409"))
        {
            HasUserZnSubbed = true;
        }
        
        if (channel == null)
        {
            Console.WriteLine("[ERROR] User channel could not be found!");
            return;
        }

        TwitchBroadcasterId = channel.Id;
        System.Console.WriteLine(TwitchBroadcasterId);

        Console.WriteLine("[INFO] Connected to Twitch API as '{0}'!", user.DisplayName);

        if (overrideToken)
        {
            File.WriteAllText(TwitchOAuthAccessTokenFilePath, TwitchOAuthAccessToken);

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    File.SetAttributes(TwitchOAuthAccessTokenFilePath, File.GetAttributes(TwitchOAuthAccessTokenFilePath) | FileAttributes.Hidden);
                    break;
                case PlatformID.Unix:
#pragma warning disable CA1416 // Validate platform compatibility
                    File.SetUnixFileMode(TwitchOAuthAccessTokenFilePath, UnixFileMode.UserRead);
#pragma warning restore CA1416 // Validate platform compatibility
                    break;
                case PlatformID.MacOSX:
                    File.SetAttributes(TwitchOAuthAccessTokenFilePath, File.GetAttributes(TwitchOAuthAccessTokenFilePath) | FileAttributes.Hidden);
                    break;
            }
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);
        
        var vm = _provider?.GetRequiredService<MainWindowViewModel>();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow
            {
                DataContext = vm
            };
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}