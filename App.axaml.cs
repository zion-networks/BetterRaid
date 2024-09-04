using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BetterRaid.Extensions;
using BetterRaid.Services;
using BetterRaid.Services.Implementations;
using BetterRaid.ViewModels;
using BetterRaid.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BetterRaid;

public class App : Application
{
    private static readonly ServiceCollection Services = [];
    private static ServiceProvider? _serviceProvider;
    
    public static IServiceProvider? ServiceProvider => _serviceProvider;

    public override void Initialize()
    {
        InitializeServices();

        AvaloniaXamlLoader.Load(_serviceProvider, this);
    }

    private void InitializeServices()
    {
        Services.AddSingleton<ITwitchDataService, TwitchDataService>();
        Services.AddSingleton<ITwitchPubSubService, TwitchPubSubService>();
        Services.AddTransient<MainWindowViewModel>();
        
        _serviceProvider = Services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);
        
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                desktop.MainWindow.InjectDataContext<MainWindowViewModel>();
                
                break;
            
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainWindow();
                singleViewPlatform.MainView.InjectDataContext<MainWindowViewModel>();
                
                break;
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}