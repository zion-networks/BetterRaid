using Avalonia.Controls;

namespace BetterRaid.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            listBox.SelectedItems?.Clear();
        }
    }
}