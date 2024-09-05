using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BetterRaid.Services;
using BetterRaid.Services.Implementations;
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
        var services = new ServiceCollection();
        services.AddSingleton<ITwitchService, TwitchService>();
        services.AddSingleton<ISynchronizaionService, DispatcherService>(serviceProvider => new DispatcherService(Dispatcher.UIThread));
        services.AddTransient<IMainViewModelFactory, MainWindowViewModelFactory>();

        return services.BuildServiceProvider();
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
        var mainWindow = new MainWindow
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