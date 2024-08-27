using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private BackgroundWorker _autoUpdater;

    public MainWindow()
    {
        _raidButtonVMs = [];
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
            vm.Database = BetterRaidDatabase.LoadFromFile("db.json");
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
    }

    private void InitializeRaidChannels()
    {
        if (_autoUpdater?.IsBusy == false)
        {
            _autoUpdater?.CancelAsync();
        }

        foreach (var rbvm in _raidButtonVMs)
        {
            
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

        if (_autoUpdater?.IsBusy == false)
        {
            _autoUpdater?.RunWorkerAsync();
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
        foreach (var vm in _raidButtonVMs)
        {
            Task.Run(vm.GetOrUpdateChannelAsync);
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