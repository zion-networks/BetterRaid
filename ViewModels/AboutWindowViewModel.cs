using System;
using BetterRaid.Services;

namespace BetterRaid.ViewModels;

public class AboutWindowViewModel : ViewModelBase
{
    public AboutWindowViewModel(ITwitchDataService s)
    {
        Console.WriteLine(s);
        Console.WriteLine("[DEBUG] AboutWindowViewModel created");
    }
}