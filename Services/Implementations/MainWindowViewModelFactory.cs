using BetterRaid.ViewModels;

namespace BetterRaid.Services.Implementations;

public class MainWindowViewModelFactory : IMainViewModelFactory
{
    private readonly ITwitchService _twitchService;
    private readonly ISynchronizaionService _synchronizaionService;

    public MainWindowViewModelFactory(ITwitchService twitchService, ISynchronizaionService synchronizaionService)
    {
        _twitchService = twitchService;
        _synchronizaionService = synchronizaionService;
    }

    public MainWindowViewModel CreateMainWindowViewModel()
    {
        return new MainWindowViewModel(_twitchService, _synchronizaionService);
    }
}
