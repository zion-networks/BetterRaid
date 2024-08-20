using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using BetterRaid.ViewModels;

namespace BetterRaid.Views;

public partial class MainWindow : Window
{
    private BackgroundWorker _autoUpdater;

    private string[] _channelNames = [
        "Cedricun", // Ehrenbruder
        "ZanTal",   // Ehrenschwester
        "PropzMaster",
        "Artimus83",
        "HyperonsLive",
        "theshroomlife",
        "Robocraft999",
        "sllikson",
        "Aron_dc",
        "AIEsports"
    ];

    public MainWindow()
    {
        _autoUpdater = new BackgroundWorker();

        InitializeComponent();
        GenerateRaidGrid();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += OnViewModelChanged;
        }
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.Filter))
        {
            if (DataContext is MainWindowViewModel mainWindowVm)
            {
                if (string.IsNullOrEmpty(mainWindowVm.Filter))
                {
                    foreach (var child in raidGrid.Children)
                    {
                        child.IsVisible = true;
                    }

                    return;
                }

                foreach (var child in raidGrid.Children)
                {
                    if (child.DataContext is RaidButtonViewModel vm)
                    {
                        if (string.IsNullOrEmpty(vm.Channel?.DisplayName))
                            continue;
                        
                        if (string.IsNullOrEmpty(mainWindowVm.Filter))
                            continue;
                        
                        if (vm.Channel.DisplayName.Contains(mainWindowVm.Filter, StringComparison.OrdinalIgnoreCase) == false)
                        {
                            child.IsVisible = false;
                        }
                    }
                }
            }
        }
    }

    private void GenerateRaidGrid()
    {
        var rows = (int)Math.Ceiling(_channelNames.Length / 3.0);

        for (var i = 0; i < rows; i++)
        {
            raidGrid.RowDefinitions.Add(new RowDefinition(GridLength.Parse("*")));
        }

        var colIndex = 0;
        var rowIndex = 0;
        foreach (var channel in _channelNames)
        {
            if (string.IsNullOrEmpty(channel))
                continue;

            var btn = new RaidButton
            {
                DataContext = new RaidButtonViewModel
                {
                    ChannelName = channel
                }
            };

            Grid.SetColumn(btn, colIndex);
            Grid.SetRow(btn, rowIndex);

            raidGrid.Children.Add(btn);

            colIndex++;
            if (colIndex % 3 == 0)
            {
                colIndex = 0;
                rowIndex++;
            }

            if (btn.DataContext is RaidButtonViewModel vm)
            {
                Dispatcher.UIThread.InvokeAsync(vm.GetOrUpdateChannelAsync);
            }
        }

        _autoUpdater.DoWork += UpdateAllTiles;
        _autoUpdater.RunWorkerAsync();
    }

    public void UpdateAllTiles(object? sender, DoWorkEventArgs e)
    {
        while (e.Cancel == false)
        {
            Task.Delay(App.AutoUpdateDelay).Wait();

            if (raidGrid == null || raidGrid.Children.Count == 0)
            {
                return;
            }

            foreach (var children in raidGrid.Children)
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (children.DataContext is RaidButtonViewModel vm)
                    {
                        await vm.GetOrUpdateChannelAsync();
                    }
                }
            );
            }

            Console.WriteLine("Data Update");
        }
    }
}