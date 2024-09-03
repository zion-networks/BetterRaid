using System;

namespace BetterRaid.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
public class PubSubAttribute : Attribute
{
    public PubSubType Type { get; }
    public string ChannelIdField { get; set; }
    
    public PubSubAttribute(PubSubType type, string channelIdField)
    {
        Type = type;
        ChannelIdField = channelIdField;
    }

}