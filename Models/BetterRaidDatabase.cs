using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace BetterRaid.Models;

[JsonObject]
public class BetterRaidDatabase : INotifyPropertyChanged
{
    [JsonIgnore]
    private string? _databaseFilePath;
    private bool _onlyOnline;

    public event PropertyChangedEventHandler? PropertyChanged;
    public bool OnlyOnline
    {
        get => _onlyOnline;
        set
        {
            if (value == _onlyOnline)
                return;
            
            _onlyOnline = value;
            OnPropertyChanged();
        }
    }
    public List<string> Channels { get; set; } = [];
    public bool AutoSave { get; set; }

    public static BetterRaidDatabase LoadFromFile(string path)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(path);

        path = Path.Combine(Environment.CurrentDirectory, path);

        if (File.Exists(path) == false)
        {
            throw new FileNotFoundException("Database file not found", path);
        }

        var dbStr = File.ReadAllText(path);
        var dbObj = JsonConvert.DeserializeObject<BetterRaidDatabase>(dbStr);

        if (dbObj == null)
        {
            throw new JsonException("Failed to read database file");
        }
        
        dbObj._databaseFilePath = path;

        Console.WriteLine("[DEBUG] Loaded database from {0}", path);

        return dbObj;
    }

    public void Save(string? path = null)
    {
        if (string.IsNullOrEmpty(_databaseFilePath) && string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("No target path given to save database at");
        }

        var dbStr = JsonConvert.SerializeObject(this);
        var targetPath = (path ?? _databaseFilePath)!;

        File.WriteAllText(targetPath, dbStr);

        Console.WriteLine("[DEBUG] Saved database to {0}", targetPath);
    }

    public void AddChannel(string channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (Channels.Contains(channel))
            return;
        
        Channels.Add(channel);
        OnPropertyChanged(nameof(Channels));
    }

    public void RemoveChannel(string channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (Channels.Contains(channel) == false)
            return;
        
        Channels.Remove(channel);
        OnPropertyChanged(nameof(Channels));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        if (AutoSave && _databaseFilePath != null)
        {
            Save();
        }
    }
}