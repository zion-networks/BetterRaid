namespace BetterRaid.Misc;

public static class Tools
{
    public static string GetOAuthUrl()
    {
        var scopes = string.Join("+", Constants.TwitchOAuthScopes);

        return $"https://id.twitch.tv/oauth2/authorize"
               + $"?client_id={Constants.TwitchClientId}"
               + $"&redirect_uri={Constants.TwitchOAuthRedirectUrl}"
               + $"&response_type={Constants.TwitchOAuthResponseType}"
               + $"&scope={scopes}";
    }
}