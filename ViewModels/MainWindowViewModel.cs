using System;
using Avalonia;
using Avalonia.Controls;
using BetterRaid.Extensions;
using BetterRaid.Models;
using BetterRaid.Views;

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

    public void ShowAboutWindow(Window owner)
    {
        var about = new AboutWindow();
        about.ShowDialog(owner);
        about.CenterToOwner();
    }
}
