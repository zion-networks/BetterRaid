namespace BetterRaid.Services;

public interface ITwitchDataService
{
    public bool IsRaidStarted { get; set; }
    
    public void StartRaid(string from, string to);
    public void StartRaidCommand(object? arg);
    public void StopRaid();
    public void StopRaidCommand();
    public void OpenChannelCommand(object? arg);
}