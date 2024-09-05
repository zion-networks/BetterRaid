using Avalonia.Threading;
using System;
using System.Threading;

namespace BetterRaid.Services.Implementations;
public class DispatcherService : ISynchronizaionService
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
