namespace BetterRaid.Services;

public interface ITwitchPubSubService
{
    void RegisterReceiver<T>(T receiver) where T : class;
    void UnregisterReceiver<T>(T receiver) where T : class;
}