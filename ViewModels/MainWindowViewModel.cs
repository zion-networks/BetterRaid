using System;
using Avalonia;
using BetterRaid.Models;

namespace BetterRaid.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string? _filter;
    private bool _onlyOnline;
    
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

    public bool OnlyOnline
    {
        get => _onlyOnline;
        set
        {
            if (SetProperty(ref _onlyOnline, value))
            {
                if (Database != null)
                    Database.OnlyOnline = value;
            }
        }
    }

    public void ExitApplication()
    {
        //TODO polish later
        Environment.Exit(0);
    }
}
