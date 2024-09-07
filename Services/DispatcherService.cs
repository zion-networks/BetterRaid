using System;
using Avalonia.Threading;

namespace BetterRaid.Services;

public interface IDispatcherService
{
    void Invoke(Action action);
}

public class DispatcherService : IDispatcherService
{
    private readonly Dispatcher dispatcher;

    public DispatcherService(Dispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }
    public void Invoke(Action action)
    {
        dispatcher.Invoke(action);
    }
}
