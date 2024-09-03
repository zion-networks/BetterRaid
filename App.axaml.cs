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
    
    private static TwitchAPI? _twitchApi;
    private static bool _hasUserZnSubbed;
    private static string _betterRaidDataPath = "";
    private static string _twitchBroadcasterId = "";
    private static string _twitchOAuthAccessToken = "";
    private static string _twitchOAuthAccessTokenFilePath = "";
    private const string TokenClientId = "kkxu4jorjrrc5jch1ito5i61hbev2o";
    private const string TwitchOAuthRedirectUrl = "http://localhost:9900";
    private const string TwitchOAuthResponseType = "token";
    
    private static readonly string[] TwitchOAuthScopes = [
        "channel:manage:raids",
        "user:read:subscriptions"
    ];
    
    internal static readonly string TwitchOAuthUrl = $"https://id.twitch.tv/oauth2/authorize"
                                                    + $"?client_id={TokenClientId}"
                                                    + $"&redirect_uri={TwitchOAuthRedirectUrl}"
                                                    + $"&response_type={TwitchOAuthResponseType}"
                                                    + $"&scope={string.Join("+", TwitchOAuthScopes)}";

    public const string ChannelPlaceholderImageUrl = "https://cdn.pixabay.com/photo/2018/11/13/22/01/avatar-3814081_1280.png";

    public static TwitchAPI? TwitchApi => _twitchApi;
    public static bool HasUserZnSubbed => _hasUserZnSubbed;
    
    public IServiceProvider? Provider => _provider;
    public static string? TwitchBroadcasterId => _twitchBroadcasterId;

    public static string TwitchOAuthAccessToken
    {
        get => _twitchOAuthAccessToken;
        set
        {
            _twitchOAuthAccessToken = value;
            InitTwitchClient(true);
        }
    }

    public override void Initialize()
    {
        InitializeServices();
        LoadTwitchToken();

        AvaloniaXamlLoader.Load(_provider, this);
    }

    private void LoadTwitchToken()
    {
        var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        _betterRaidDataPath = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => Path.Combine(userHomeDir, "AppData", "Roaming", "BetterRaid"),
            PlatformID.Unix => Path.Combine(userHomeDir, ".config", "BetterRaid"),
            PlatformID.MacOSX => Path.Combine(userHomeDir, "Library", "Application Support", "BetterRaid"),
            _ => throw new PlatformNotSupportedException($"Your platform '{Environment.OSVersion.Platform}' is not supported. Please report this issue here: https://www.github.com/zion-networks/BetterRaid/issues")
        };

        if (!Directory.Exists(_betterRaidDataPath))
        {
            var di = Directory.CreateDirectory(_betterRaidDataPath);
            if (di.Exists == false)
            {
                throw new Exception($"Failed to create directory '{_betterRaidDataPath}'");
            }
        }

        _twitchOAuthAccessTokenFilePath = Path.Combine(_betterRaidDataPath, ".access_token");

        if (!File.Exists(_twitchOAuthAccessTokenFilePath))
            return;
        
        _twitchOAuthAccessToken = File.ReadAllText(_twitchOAuthAccessTokenFilePath);
        InitTwitchClient();
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
        
        if (string.IsNullOrEmpty(_twitchOAuthAccessToken))
        {
            Console.WriteLine("[ERROR] Failed to initialize Twitch Client: Access Token is empty!");
            return;
        }

        _twitchApi = new TwitchAPI
        {
            Settings =
            {
                ClientId = TokenClientId,
                AccessToken = _twitchOAuthAccessToken
            }
        };

        Console.WriteLine("[INFO] Testing Twitch API connection...");

        var user = _twitchApi.Helix.Users.GetUsersAsync().Result.Users.FirstOrDefault();
        if (user == null)
        {
            _twitchApi = null;
            Console.WriteLine("[ERROR] Failed to connect to Twitch API!");
            return;
        }

        var channel = _twitchApi.Helix.Search
                        .SearchChannelsAsync(user.Login).Result.Channels
                        .FirstOrDefault(c => c.BroadcasterLogin == user.Login);
        
        var userSubs = _twitchApi.Helix.Subscriptions.CheckUserSubscriptionAsync(
            userId: user.Id,
            broadcasterId: "1120558409"
        ).Result.Data;
        
        if (userSubs is { Length: > 0 } && userSubs.Any(s => s.BroadcasterId == "1120558409"))
        {
            _hasUserZnSubbed = true;
        }
        
        if (channel == null)
        {
            Console.WriteLine($"[ERROR] Failed to get channel information for '{user.Login}'!");
            return;
        }

        _twitchBroadcasterId = channel.Id;
        Console.WriteLine(_twitchBroadcasterId);

        Console.WriteLine("[INFO] Connected to Twitch API as '{0}'!", user.DisplayName);

        if (!overrideToken)
            return;
        
        File.WriteAllText(_twitchOAuthAccessTokenFilePath, _twitchOAuthAccessToken);

        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
                File.SetAttributes(_twitchOAuthAccessTokenFilePath, File.GetAttributes(_twitchOAuthAccessTokenFilePath) | FileAttributes.Hidden);
                break;
            
            case PlatformID.Unix:
#pragma warning disable CA1416 // Validate platform compatibility
                File.SetUnixFileMode(_twitchOAuthAccessTokenFilePath, UnixFileMode.UserRead);
#pragma warning restore CA1416 // Validate platform compatibility
                break;
            
            case PlatformID.MacOSX:
                File.SetAttributes(_twitchOAuthAccessTokenFilePath, File.GetAttributes(_twitchOAuthAccessTokenFilePath) | FileAttributes.Hidden);
                break;

            default:
                throw new PlatformNotSupportedException($"Your platform '{Environment.OSVersion.Platform}' is not supported. Please report this issue here: https://www.github.com/zion-networks/BetterRaid/issues");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);
        
        var vm = _provider?.GetRequiredService<MainWindowViewModel>();
        
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
            
                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm
                };
                break;
            
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainWindow
                {
                    DataContext = vm
                };
                break;
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}