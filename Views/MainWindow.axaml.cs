using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BetterRaid.Extensions;
using BetterRaid.Models;
using BetterRaid.ViewModels;

namespace BetterRaid.Views;

public partial class MainWindow : Window
{
    private ObservableCollection<RaidButtonViewModel> _raidButtonVMs;
    private RaidButtonViewModel? _znButtonVm;
    private BackgroundWorker _autoUpdater;

    public MainWindow()
    {
        _raidButtonVMs = [];
        _znButtonVm = null;
        _autoUpdater = new();

        DataContextChanged += OnDataContextChanged;
        
        InitializeComponent();

        _autoUpdater.WorkerSupportsCancellation = true;
        _autoUpdater.DoWork += UpdateAllTiles;
    }

    private void OnDatabaseChanged(object? sender, PropertyChangedEventArgs e)
    {
        // TODO: Only if new channel was added or existing were removed
        // InitializeRaidChannels();
        GenerateRaidGrid();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var dbPath = Path.Combine(App.BetterRaidDataPath, "db.json");
        
            try
            {
                vm.Database = BetterRaidDatabase.LoadFromFile(dbPath);
            }
            catch (FileNotFoundException)
            {
                var db = new BetterRaidDatabase();
                db.Save(dbPath);

                vm.Database = db;
            }
            
            vm.Database.AutoSave = true;
            vm.Database.PropertyChanged += OnDatabaseChanged;

            vm.PropertyChanged += OnViewModelChanged;

            InitializeRaidChannels();
            GenerateRaidGrid();
        }
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.Filter))
        {
            GenerateRaidGrid();
        }

        if (e.PropertyName == nameof(MainWindowViewModel.IsLoggedIn) && DataContext is MainWindowViewModel { IsLoggedIn: true })
        {
            InitializeRaidChannels();
            GenerateRaidGrid();
        }
    }

    private void InitializeRaidChannels()
    {
        if (DataContext is MainWindowViewModel { IsLoggedIn: false })
            return;

        if (_autoUpdater?.IsBusy == false)
        {
            _autoUpdater?.CancelAsync();
        }

        _raidButtonVMs.Clear();

        var vm = DataContext as MainWindowViewModel;

        if (vm?.Database == null)
            return;

        foreach (var channel in vm.Database.Channels)
        {
            if (string.IsNullOrEmpty(channel))
                continue;

            var rbvm = new RaidButtonViewModel(channel)
            {
                MainVm = vm
            };

            _raidButtonVMs.Add(rbvm);
        }

        if (App.HasUserZnSubbed)
        {
            _znButtonVm = null;
        }
        else
        {
            _znButtonVm = new RaidButtonViewModel("zionnetworks")
            {
                MainVm = vm,
                HideDeleteButton = true,
                IsAd = true
            };
        }

        if (_autoUpdater?.IsBusy == false)
        {
            _autoUpdater?.RunWorkerAsync();
        }
    }

    private void GenerateRaidGrid()
    {
        if (DataContext is MainWindowViewModel { IsLoggedIn: false })
            return;

        foreach (var child in raidGrid.Children)
        {
            if (child is Button btn)
            {
                btn.Click -= OnAddChannelButtonClicked;
            }
        }

        raidGrid.Children.Clear();

        var vm = DataContext as MainWindowViewModel;

        if (vm?.Database == null)
        {
            return;
        }

        var visibleChannels = _raidButtonVMs.Where(channel =>
        {
            var visible = true;
            if (string.IsNullOrWhiteSpace(vm.Filter) == false)
            {
                if (channel.ChannelName.Contains(vm.Filter, StringComparison.OrdinalIgnoreCase) == false)
                {
                    visible = false;
                }
            }

            if (vm.Database.OnlyOnline && channel.Channel?.IsLive == false)
            {
                visible = false;
            }

            return visible;
        }).OrderByDescending(c => c.Channel?.IsLive).ToList();

        var rows = (int)Math.Ceiling((visibleChannels.Count + (App.HasUserZnSubbed ? 1 : 2)) / 3.0);

        for (var i = 0; i < rows; i++)
        {
            raidGrid.RowDefinitions.Add(new RowDefinition(GridLength.Parse("Auto")));
        }

        var colIndex = App.HasUserZnSubbed ? 0 : 1;
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
        }

        var addButton = new Button
        {
            Content = "+",
            FontSize = 72,
            Margin = new Avalonia.Thickness(5),
            MinHeight = 250,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        addButton.Click += OnAddChannelButtonClicked;

        Grid.SetColumn(addButton, colIndex);
        Grid.SetRow(addButton, rowIndex);

        raidGrid.Children.Add(addButton);

        if (App.HasUserZnSubbed == false)
        {
            var znButton = new RaidButton
            {
                DataContext = _znButtonVm
            };

            Grid.SetColumn(znButton, 0);
            Grid.SetRow(znButton, 0);
            raidGrid.Children.Add(znButton);
        }
    }

    private void OnAddChannelButtonClicked(object? sender, RoutedEventArgs e)
    {
        var dialog = new AddChannelWindow();
        dialog.CenterToOwner();

        var vm = DataContext as MainWindowViewModel;

        if (vm?.Database == null)
            return;
        
        // TODO Button Command not working, Button remains disabled
        // This is a dirty workaround
        dialog.okBtn.Click += (sender, args) => {
            if (string.IsNullOrWhiteSpace(dialog?.channelNameTxt.Text) == false)
            {
                vm.Database.AddChannel(dialog.channelNameTxt.Text);
                vm.Database.Save();
            }
            
            dialog?.Close();

            InitializeRaidChannels();
            GenerateRaidGrid();
        };

        dialog.ShowDialog(this);
    }

    public void UpdateChannelData()
    {
        var loggedIn = Dispatcher.UIThread.Invoke(() => {
            return (DataContext as MainWindowViewModel)?.IsLoggedIn ?? false;
        });

        if (loggedIn == false)
            return;

        foreach (var vm in _raidButtonVMs)
        {
            Task.Run(vm.GetOrUpdateChannelAsync);
        }

        if (_znButtonVm != null)
        {
            Task.Run(_znButtonVm.GetOrUpdateChannelAsync);
        }
    }

    private void UpdateAllTiles(object? sender, DoWorkEventArgs e)
    {
        while (e.Cancel == false)
        {
            UpdateChannelData();
            Task.Delay(App.AutoUpdateDelay).Wait();
        }
    }
}