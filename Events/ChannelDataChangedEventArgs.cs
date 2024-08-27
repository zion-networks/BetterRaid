using System;

namespace BetterRaid.Events;

public class ChannelDataChangedEventArgs : EventArgs
{
    public static ChannelDataChangedEventArgs FromViewerCount(int old, int now)
    {
        return new ChannelDataChangedEventArgs();
    }

    public static ChannelDataChangedEventArgs FromIsLive(bool old, bool now)
    {
        return new ChannelDataChangedEventArgs();
    }
}