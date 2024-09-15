using System;

namespace BetterRaid.Models.Events;

public class RaidStartedEventArgs
{
    public DateTime RaidTime { get; }
    public TwitchChannel Target { get; }
    
    public RaidStartedEventArgs(DateTime raidTime, TwitchChannel target)
    {
        RaidTime = raidTime;
        Target = target;
    }
}