using BetterRaid.ViewModels;

namespace BetterRaid.Services;

public interface IMainViewModelFactory
{
    MainWindowViewModel CreateMainWindowViewModel();
}

public class MainWindowViewModelFactory// : IMainViewModelFactory
{
    private readonly ITwitchService _twitchService;
    private readonly IDispatcherService _dispatcherService;

    public MainWindowViewModelFactory(ITwitchService twitchService, IDispatcherService dispatcherService)
    {
        _twitchService = twitchService;
        _dispatcherService = dispatcherService;
    }

    //public MainWindowViewModel CreateMainWindowViewModel()
    //{
    //    return new MainWindowViewModel(_twitchService, _synchronizaionService);
    //}
}
