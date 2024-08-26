using System;
using Avalonia;
using BetterRaid.Models;

namespace BetterRaid.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string? _filter;
    
    private BetterRaidDatabase? _db;

    public BetterRaidDatabase? Database
    {
        get => _db;
        set => SetProperty(ref _db, value);
    }

    public string? Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public void ExitApplication()
    {
        //TODO polish later
        Environment.Exit(0);
    }
}
