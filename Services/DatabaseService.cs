using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BetterRaid.Misc;
using BetterRaid.Models;
using BetterRaid.Models.Database;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BetterRaid.Services;

public interface IDatabaseService
{
    bool OnlyOnline { get; set; }
    bool AutoSave { get; }
    BetterRaidDatabase? Database { get; set; }
    void LoadOrCreate();
    void LoadFromFile(string path, bool createIfNotExist = false);
    Task UpdateLoadedChannels();
    void Save(string? path = null);
    bool TrySetRaided(TwitchChannel channel, DateTime dateTime);
}

public class DatabaseService : IDatabaseService, INotifyPropertyChanged
{
    private string? _databaseFilePath;
    private readonly ILogger<DatabaseService> _logger;
    private readonly ITwitchService _twitch;
    private bool _autoSave = true;

    public event PropertyChangedEventHandler? PropertyChanged;
    
    public bool OnlyOnline { get; set; }

    public bool AutoSave
    {
        get => _autoSave;
        private set
        {
            if (SetField(ref _autoSave, value) && Database is not null)
            {
                if (value)
                {
                    Database.PropertyChanged += OnDatabaseChanged;
                }
                else
                {
                    Database.PropertyChanged -= OnDatabaseChanged;
                }
            }
        }
    }

    public BetterRaidDatabase? Database { get; set; }

    public DatabaseService(ILogger<DatabaseService> logger, ITwitchService twitch)
    {
        _logger = logger;
        _twitch = twitch;
        
        _twitch.TwitchChannelUpdated += OnTwitchChannelUpdated;
    }

    private void OnTwitchChannelUpdated(object? sender, TwitchChannel e)
    {
        if (Database == null)
            return;

        if (AutoSave)
            Save();
    }

    public void LoadOrCreate()
    {
        LoadFromFile(Constants.DatabaseFilePath, true);
    }
    
    public void LoadFromFile(string path, bool createIfNotExist = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        path = Path.Combine(Environment.CurrentDirectory, path);
        var exists = File.Exists(path);

        switch (exists)
        {
            case false when createIfNotExist == false:
                throw new FileNotFoundException("Database file not found", path);
            
            case false when createIfNotExist:
                _logger.LogWarning("Database file not found, creating new database");

                Database = new BetterRaidDatabase
                {
                    AutoVisitChannelOnRaid = true,
                    OnlyOnline = false,
                    ShowUserViewerCount = true
                };
                
                Database.Channels.Add(new TwitchChannel("ZionNetworks"));
                Save(path);
            
                _logger.LogDebug("Created new database at {path}", path);

                if (AutoSave)
                {
                    Database.PropertyChanged += OnDatabaseChanged;
                }
            
                return;
            
            case true:
                var dbStr = File.ReadAllText(path);
                var dbObj = JsonConvert.DeserializeObject<BetterRaidDatabase>(dbStr);

                _databaseFilePath = path;
                Database = dbObj ?? throw new JsonException("Failed to read database file");
                
                if (AutoSave)
                {
                    Database.PropertyChanged += OnDatabaseChanged;
                }

                _logger.LogDebug("Loaded database from {path}", path);

                return;
        }
    }

    public async Task UpdateLoadedChannels()
    {
        if (Database == null || Database.Channels.Count == 0)
            return;

        await Parallel.ForAsync(0, Database.Channels.Count, (i, c) =>
        {
            if (c.IsCancellationRequested)
                return ValueTask.FromCanceled(c);
            
            var channel = Database.Channels[i];
            channel.UpdateChannelData(_twitch);
            
            return ValueTask.CompletedTask;
        });
    }

    public void Save(string? path = null)
    {
        if (string.IsNullOrEmpty(_databaseFilePath) && string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("No target path given to save database at");
        }

        if (string.IsNullOrEmpty(path) == false && string.IsNullOrEmpty(_databaseFilePath))
        {
            _databaseFilePath = path;
        }

        var dbStr = JsonConvert.SerializeObject(Database, Formatting.Indented);
        var targetPath = _databaseFilePath!;

        File.WriteAllText(targetPath, dbStr);

        _logger.LogDebug("Saved database to {targetPath}", targetPath);
    }

    public void AddChannel(TwitchChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (Database == null)
            throw new InvalidOperationException("Database is not loaded");

        if (Database.Channels.Any(c => c.Name?.Equals(c.Name, StringComparison.CurrentCultureIgnoreCase) == true))
            return;
        
        if (AutoSave)
            Save();
        
        Database.Channels.Add(channel);
    }

    public void RemoveChannel(TwitchChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (Database == null)
            throw new InvalidOperationException("Database is not loaded");
        
        var index = Database.Channels.FindIndex(c => c.Name?.Equals(c.Name, StringComparison.CurrentCultureIgnoreCase) == true);
        
        if (index == -1)
            return;
        
        if (AutoSave)
            Save();
        
        Database.Channels.RemoveAt(index);
    }

    public bool TrySetRaided(TwitchChannel channel, DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (Database == null)
            throw new InvalidOperationException("Database is not loaded");
        
        var twitchChannel = Database.Channels.FirstOrDefault(c => c.Name?.Equals(channel.Name, StringComparison.CurrentCultureIgnoreCase) == true);

        if (twitchChannel == null)
            return false;
        
        twitchChannel.LastRaided = dateTime;
        
        if (AutoSave)
            Save();
        
        return true;
    }

    private void OnDatabaseChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (AutoSave)
            Save();
    }

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