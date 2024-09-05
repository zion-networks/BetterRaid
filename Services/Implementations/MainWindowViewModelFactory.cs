using BetterRaid.ViewModels;

namespace BetterRaid.Services.Implementations;

public class MainWindowViewModelFactory : IMainViewModelFactory
{
    private readonly ITwitchPubSubService twitchPubSubService;
    private readonly ITwitchDataService twitchDataService;
    private readonly ISynchronizaionService synchronizaionService;

    public MainWindowViewModelFactory(ITwitchPubSubService twitchPubSubService, ITwitchDataService twitchDataService, ISynchronizaionService synchronizaionService)
    {
        this.twitchPubSubService = twitchPubSubService;
        this.twitchDataService = twitchDataService;
        this.synchronizaionService = synchronizaionService;
    }

    public MainWindowViewModel CreateMainWindowViewModel()
    {
        return new MainWindowViewModel(twitchPubSubService, twitchDataService, synchronizaionService);
    }
}
