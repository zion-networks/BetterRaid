using System;
using System.IO;

namespace BetterRaid.Misc;

public static class Constants
{
    public const string AppName = "BetterRaid";
    public const string AppVersion = "0.0.2.1-alpha";
    
    public const string AppWindowTitle = AppName + " v" + AppVersion;
    
    // General
    public const string ChannelPlaceholderImageUrl = "https://cdn.pixabay.com/photo/2018/11/13/22/01/avatar-3814081_1280.png";
    
    // Paths
    public static string BetterRaidDataPath => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming", "BetterRaid"),
        PlatformID.Unix => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "BetterRaid"),
        PlatformID.MacOSX => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "BetterRaid"),
        _ => throw new PlatformNotSupportedException($"Your platform '{Environment.OSVersion.Platform}' is not supported. Please report this issue here: https://www.github.com/zion-networks/BetterRaid/issues")
    };
    public static string TwitchOAuthAccessTokenFilePath => Path.Combine(BetterRaidDataPath, ".access_token");
    public static string DatabaseFilePath => Path.Combine(BetterRaidDataPath, "brdb.json");
    
    // Twitch API
    public const string TwitchClientId = "kkxu4jorjrrc5jch1ito5i61hbev2o";
    public const string TwitchOAuthRedirectUrl = "http://localhost:9900";
    public const string TwitchOAuthResponseType = "token";
    public const float RaidDuration = 90;

    public static readonly string[] TwitchOAuthScopes = [
        "channel:manage:raids",     // Allows the application to start and cancel raids on the broadcaster's channel
        "user:read:subscriptions"   // Allows the application to check, if the user has subscribed to the developer's channel
    ];
}