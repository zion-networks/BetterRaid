using System;
using Avalonia;

namespace BetterRaid.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string? _filter;

    public string? Filter
    {
        get => _filter;
        set
        {
            if (value == _filter)
                return;
            
            _filter = value;
            OnPropertyChanged();
        }
    }

    public void ExitApplication()
    {
        //TODO polish later
        Environment.Exit(0);
    }
}
