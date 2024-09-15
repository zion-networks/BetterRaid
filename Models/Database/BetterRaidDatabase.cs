using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace BetterRaid.Models.Database;

[JsonObject]
public class BetterRaidDatabase : INotifyPropertyChanged
{
    #region Private Fields
    
    private bool _onlyOnline;
    private bool _showUserViewerCount = true;
    private bool _autoVisitChannelOnRaid = true;

    #endregion Private Fields

    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion Events
    
    #region Settings

    public bool OnlyOnline
    {
        get => _onlyOnline;
        set => SetField(ref _onlyOnline, value);
    }

    public bool ShowUserViewerCount
    {
        get => _showUserViewerCount;
        set => SetField(ref _showUserViewerCount, value);
    }

    public bool AutoVisitChannelOnRaid
    {
        get => _autoVisitChannelOnRaid;
        set => SetField(ref _autoVisitChannelOnRaid, value);
    }

    #endregion Settings
    
    #region Data
    
    public List<TwitchChannel> Channels { get; set; } = [];

    #endregion Data

    #region Event Handlers

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion Event Handlers
}