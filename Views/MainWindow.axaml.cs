using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BetterRaid.ViewModels;

namespace BetterRaid.Views;

public partial class MainWindow : Window
{
    private ObservableCollection<RaidButtonViewModel> _raidButtonVMs;
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
        _raidButtonVMs = [];
        _autoUpdater = new();

        InitializeComponent();
        InitializeRaidChannels();
        GenerateRaidGrid();

        DataContextChanged += OnDataContextChanged;

        _autoUpdater.DoWork += UpdateAllTiles;
        _autoUpdater.RunWorkerAsync();
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
                    foreach (var vm in _raidButtonVMs)
                    {
                        vm.ShowInGrid = true;
                    }
                }

                foreach (var vm in _raidButtonVMs)
                {
                    if (string.IsNullOrEmpty(mainWindowVm.Filter))
                        continue;

                    if (string.IsNullOrEmpty(vm.Channel?.DisplayName))
                        continue;

                    vm.ShowInGrid = vm.Channel.DisplayName.Contains(mainWindowVm.Filter, StringComparison.OrdinalIgnoreCase);
                }
            }

            GenerateRaidGrid();
        }

        if (e.PropertyName == nameof(MainWindowViewModel.OnlyOnline))
        {
            if (DataContext is MainWindowViewModel mainWindowVm)
            {
                foreach (var vm in _raidButtonVMs)
                {
                    if (mainWindowVm.OnlyOnline)
                    {
                        vm.ShowInGrid = vm.Channel.IsLive;
                    }
                    else
                    {
                        vm.ShowInGrid = true;
                    }
                }
            }

            GenerateRaidGrid();
        }
    }

    private void InitializeRaidChannels()
    {
        _raidButtonVMs.Clear();

        foreach (var channel in _channelNames)
        {
            if (string.IsNullOrEmpty(channel))
                continue;

            _raidButtonVMs.Add(new RaidButtonViewModel
            {
                ChannelName = channel
            });
        }
    }

    private void GenerateRaidGrid()
    {
        foreach (var child in raidGrid.Children)
        {
            if (child is Button btn)
            {
                btn.Click -= OnAddChannelButtonClicked;
            }
        }

        raidGrid.Children.Clear();

        var visibleChannels = _raidButtonVMs.Where(c => c.ShowInGrid).ToList();
        var rows = (int)Math.Ceiling((visibleChannels.Count + 1) / 3.0);

        for (var i = 0; i < rows; i++)
        {
            raidGrid.RowDefinitions.Add(new RowDefinition(GridLength.Parse("Auto")));
        }

        var colIndex = 0;
        var rowIndex = 0;
        foreach (var channel in visibleChannels)
        {
            var btn = new RaidButton
            {
                DataContext = channel
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

        var addButton = new Button
        {
            Content = "+",
            FontSize = 36,
            Margin = new Avalonia.Thickness(5),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        addButton.Click += OnAddChannelButtonClicked;

        Grid.SetColumn(addButton, colIndex);
        Grid.SetRow(addButton, rowIndex);

        raidGrid.Children.Add(addButton);
    }

    private void OnAddChannelButtonClicked(object? sender, RoutedEventArgs e)
    {
        var dialog = new AddChannelWindow();
        dialog.Position = new Avalonia.PixelPoint(
            (int)(Position.X + Width / 2 - dialog.Width / 2),
            (int)(Position.Y + Height / 2 - dialog.Height / 2)
        );
        
        // TODO Button Command not working, Button remains disabled
        // This is a dirty workaround
        dialog.okBtn.Click += (sender, args) => {
            Array.Resize(ref _channelNames, _channelNames.Length + 1);
            _channelNames[^1] = dialog?.channelNameTxt.Text ?? "";
            dialog?.Close();

            InitializeRaidChannels();
            GenerateRaidGrid();
        };

        dialog.ShowDialog(this);
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
        }
    }
}