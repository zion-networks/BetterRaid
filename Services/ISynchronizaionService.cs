using System;

namespace BetterRaid.Services;

public interface ISynchronizaionService
{
    void Invoke(Action action);
}
