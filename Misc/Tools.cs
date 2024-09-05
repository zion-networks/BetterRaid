using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using BetterRaid.Services;

namespace BetterRaid.Misc;

public static class Tools
{
    private static HttpListener? _oauthListener;

    // Source: https://stackoverflow.com/a/43232486
    public static void StartOAuthLogin(string url, ITwitchDataService twitchDataService,  Action? callback = null, CancellationToken token = default)
    {
        if (_oauthListener == null)
        {
            _oauthListener = new HttpListener();
            _oauthListener.Prefixes.Add(Constants.TwitchOAuthRedirectUrl + "/");
            _oauthListener.Start();

            Task.Run(() => WaitForCallback(callback, token, twitchDataService), token);
        }

        OpenUrl(url);
    }

    private static async Task WaitForCallback(Action? callback, CancellationToken token, ITwitchDataService twitchDataService)
    {
        if (_oauthListener == null)
            return;

        if (token.IsCancellationRequested)
            return;

        Console.WriteLine("Starting token listener");

        while (!token.IsCancellationRequested)
        {
            var ctx = await _oauthListener.GetContextAsync();
            var req = ctx.Request;
            var res = ctx.Response;

            if (req.Url == null)
                continue;

            Console.WriteLine("{0} {1}", req.HttpMethod, req.Url);

            // Response, that may contain the access token as fragment
            // It must be extracted client-side in browser
            if (req.Url.LocalPath == "/")
            {
                var buf = new byte[1024];
                var data = new StringBuilder();
                int bytesRead;
                while ((bytesRead = await req.InputStream.ReadAsync(buf, token)) > 0)
                {
                    data.Append(Encoding.UTF8.GetString(buf, 0, bytesRead));
                }

                req.InputStream.Close();

                Console.WriteLine(data.ToString());

                res.StatusCode = 200;
                await res.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(OAUTH_CLIENT_DOCUMENT).AsMemory(0, OAUTH_CLIENT_DOCUMENT.Length), token);
                res.Close();
            }

            if (req.Url.LocalPath == "/login")
            {
                var buf = new byte[1024];
                var data = new StringBuilder();
                int bytesRead;
                while ((bytesRead = await req.InputStream.ReadAsync(buf, token)) > 0)
                {
                    data.Append(Encoding.UTF8.GetString(buf, 0, bytesRead));
                }

                req.InputStream.Close();

                var json = data.ToString();
                var jsonData = JsonNode.Parse(json);

                if (jsonData == null)
                {
                    Console.WriteLine("[ERROR] Failed to parse JSON data:");
                    Console.WriteLine(json);
                    
                    res.StatusCode = 400;
                    res.Close();
                    continue;
                }

                if (jsonData["access_token"] == null)
                {
                    Console.WriteLine("[ERROR] Missing access_token in JSON data.");
                    
                    res.StatusCode = 400;
                    res.Close();
                    continue;
                }

                var accessToken = jsonData["access_token"]?.ToString();
                
                twitchDataService.ConnectApiAsync(Constants.TwitchClientId, accessToken!);                

                res.StatusCode = 200;
                res.Close();

                Console.WriteLine("[INFO] Received access token!");

                callback?.Invoke();

                _oauthListener.Stop();
                return;
            }
        }
    }

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    private const string OAUTH_CLIENT_DOCUMENT =
@"
<!DOCTYPE html>
<header>
    <title>BetterRaid Twitch Login</title>
</header>
<body>
    <h1>Successfully logged in!</h1>

    <script>
        var urlParams = new URLSearchParams(window.location.hash.substr(1));
        var accessToken = urlParams.get('access_token');

        var xhr = new XMLHttpRequest();
        xhr.open('POST', 'http://localhost:9900/login', true);
        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.send(JSON.stringify({ access_token: accessToken }));
    </script>
</body>
";
}