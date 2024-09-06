using System.Collections.Generic;
using Newtonsoft.Json;

namespace BetterRaid.Models.Database;

[JsonObject]
public class BetterRaidDatabase
{
    public bool OnlyOnline { get; set; }
    public List<TwitchChannel> Channels { get; set; } = [];
}