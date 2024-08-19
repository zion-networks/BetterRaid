namespace BetterRaid.Models;

public class TwitchChannel
{
    public string? BroadcasterId { get; set; }
    public string Name { get; set; }
    public bool IsLive { get; set; }
    public int ViewerCount { get; set; }

    public TwitchChannel(string channelName)
    {
        Name = channelName;
    }
}