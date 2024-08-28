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

namespace BetterRaid;

public partial class App : Application
{
    internal static TwitchAPI? TwitchApi = null;
    internal static int AutoUpdateDelay = 10_000;
    internal static string TwitchOAuthAccessToken = "";
    internal static string TwitchOAuthAccessTokenFilePath = "";
    internal static string TokenClientId = "kkxu4jorjrrc5jch1ito5i61hbev2o";
    internal static readonly string TwitchOAuthRedirectUrl = "http://localhost:9900";
    internal static readonly string TwitchOAuthResponseType = "token";
    internal static readonly string[] TwitchOAuthScopes = [ "channel:manage:raids", "user:read:chat" ];
    internal static readonly string TwitchOAuthUrl = $"https://id.twitch.tv/oauth2/authorize"
                                                    + $"?client_id={TokenClientId}"
                                                    + "&redirect_uri=http://localhost:9900"
                                                    + $"&response_type={TwitchOAuthResponseType}"
                                                    + $"&scope={string.Join("+", TwitchOAuthScopes)}";

    public override void Initialize()
    {
        var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var betterRaidDir = "";

        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
                betterRaidDir = Path.Combine(userHomeDir, "AppData", "Roaming", "BetterRaid");
                break;
            case PlatformID.Unix:
                betterRaidDir = Path.Combine(userHomeDir, ".config", "BetterRaid");
                break;
            case PlatformID.MacOSX:
                betterRaidDir = Path.Combine(userHomeDir, "Library", "Application Support", "BetterRaid");
                break;
        }

        if (!Directory.Exists(betterRaidDir))
            Directory.CreateDirectory(betterRaidDir);

        TwitchOAuthAccessTokenFilePath = Path.Combine(betterRaidDir, ".access_token");

        if (File.Exists(TwitchOAuthAccessTokenFilePath))
        {
            TwitchOAuthAccessToken = File.ReadAllText(TwitchOAuthAccessTokenFilePath);
            InitTwitchClient();
        }


        AvaloniaXamlLoader.Load(this);
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