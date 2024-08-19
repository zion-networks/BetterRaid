using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BetterRaid.ViewModels;
using BetterRaid.Views;

namespace BetterRaid;

public partial class App : Application
{
    public static string TokenClientId = "";
    public static string TokenClientSecret = "";

    public override void Initialize()
    {
        try
        {
            var tokenFile      = "zn_twitch.secret";
            var profilePath    = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var tokenFilePath  = Path.Combine(profilePath, tokenFile);
            var tokenFileLines = File.ReadAllLines(tokenFilePath);
            TokenClientId      = tokenFileLines[0].Split('=')[1];
            TokenClientSecret  = tokenFileLines[1].Split('=')[1];
        }
        catch (Exception)
        {
            Console.WriteLine("[ERROR] Failed to read token from secret file!");
            Environment.Exit(1);
        }

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}