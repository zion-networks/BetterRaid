using System;
using Avalonia;

namespace BetterRaid.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string? _filter;
    private bool _onlyOnline;

    public string? Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public bool OnlyOnline
    {
        get => _onlyOnline;
        set => SetProperty(ref _onlyOnline, value);
    }

    public void ExitApplication()
    {
        //TODO polish later
        Environment.Exit(0);
    }
}
