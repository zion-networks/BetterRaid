using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BetterRaid.Extensions;
using BetterRaid.Services;
using BetterRaid.Services.Implementations;
using BetterRaid.ViewModels;
using BetterRaid.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BetterRaid;

public class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        _serviceProvider = InitializeServices();
        AvaloniaXamlLoader.Load(_serviceProvider, this);
    }

    private ServiceProvider InitializeServices()
    {
        var Services = new ServiceCollection();
        Services.AddSingleton<ITwitchDataService, TwitchDataService>();
        Services.AddSingleton<ITwitchPubSubService, TwitchPubSubService>();
        Services.AddSingleton<ISynchronizaionService, DispatcherService>(serviceProvider => new DispatcherService(Dispatcher.UIThread));
        Services.AddTransient<IMainViewModelFactory, MainWindowViewModelFactory>();

        return Services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        if(_serviceProvider == null)
        {
            throw new FieldAccessException($"\"{nameof(_serviceProvider)}\" was null");
        }

        var viewModelFactory = _serviceProvider.GetRequiredService<IMainViewModelFactory>();
        var mainWindowViewModel = viewModelFactory.CreateMainWindowViewModel();
        var mainWindow = new MainWindow()
        {
            DataContext = mainWindowViewModel
        };

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = mainWindow;
                break;

            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = mainWindow;
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}