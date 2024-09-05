using BetterRaid.ViewModels;

namespace BetterRaid.Services;

public interface IMainViewModelFactory
{
    MainWindowViewModel CreateMainWindowViewModel();
}
