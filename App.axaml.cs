using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BetterRaid.Services;
using BetterRaid.ViewModels;
using BetterRaid.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BetterRaid;

public class App : Application
{
    private ServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;

    public override void Initialize()
    {
        _serviceProvider = InitializeServices();
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();

        if (TryLoadDatabase() == false)
        {
            _logger?.LogError("Failed to load or initialize database");
            
            Environment.Exit(1);
        }

        AvaloniaXamlLoader.Load(_serviceProvider, this);
    }

    private bool TryLoadDatabase()
    {
        if (_serviceProvider == null)
        {
            throw new FieldAccessException($"\"{nameof(_serviceProvider)}\" was null");
        }
        
        var db = _serviceProvider.GetRequiredService<IDatabaseService>();

        try
        {
            db.LoadOrCreate();
            Task.Run(db.UpdateLoadedChannels);

            return true;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to load database");
            return false;
        }
    }

    private ServiceProvider InitializeServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddConsole();
        });
        services.AddSingleton<ITwitchService, TwitchService>();
        services.AddSingleton<IWebToolsService, WebToolsService>();
        services.AddSingleton<IDatabaseService, DatabaseService>();
        services.AddSingleton<IDispatcherService, DispatcherService>(_ => new DispatcherService(Dispatcher.UIThread));
        services.AddTransient<ChannelsPageViewModel>();
        services.AddTransient<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAt(0);

        if(_serviceProvider == null)
        {
            throw new FieldAccessException($"\"{nameof(_serviceProvider)}\" was null");
        }

        var vm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow
        {
            DataContext = vm
        };

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = mainWindow;
                desktop.Exit += OnDesktopOnExit;
                break;

            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = mainWindow;
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopOnExit(object? o, ControlledApplicationLifetimeExitEventArgs controlledApplicationLifetimeExitEventArgs)
    {
        if (_serviceProvider == null)
        {
            throw new FieldAccessException($"\"{nameof(_serviceProvider)}\" was null");
        }
        
        try
        {
            var db = _serviceProvider.GetRequiredService<IDatabaseService>();
            db.Save();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to save database");
        }
    }
}