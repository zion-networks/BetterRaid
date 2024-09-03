using System.Reflection;

namespace BetterRaid.Models;

public class PubSubListener
{
    public string ChannelId { get; set; }
    public object? Instance { get; set; }
    public MemberInfo? Listener { get; set; }
}