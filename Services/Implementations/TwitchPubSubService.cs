using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BetterRaid.Attributes;
using BetterRaid.Extensions;
using BetterRaid.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace BetterRaid.Services.Implementations;

public class TwitchPubSubService : ITwitchPubSubService
{
    private readonly Dictionary<PubSubType, List<PubSubListener>> _targets = new();
    private readonly ITwitchDataService _dataService;
    private TwitchPubSub? _sub;

    public TwitchPubSubService(ITwitchDataService dataService)
    {
        _dataService = dataService;

        Task.Run(InitializePubSubAsync);
    }

    private async Task InitializePubSubAsync()
    {
        while (_dataService.UserChannel == null)
        {
            await Task.Delay(100);
        }
        
        _sub = new TwitchPubSub();

        _sub.OnPubSubServiceConnected += OnSubOnOnPubSubServiceConnected;
        _sub.OnPubSubServiceError += OnSubOnOnPubSubServiceError;
        _sub.OnPubSubServiceClosed += OnSubOnOnPubSubServiceClosed;
        _sub.OnListenResponse += OnSubOnOnListenResponse;
        _sub.OnViewCount += OnSubOnOnViewCount;
        _sub.OnStreamUp += OnStreamUp;
        _sub.OnStreamDown += OnStreamDown;

        _sub.Connect();

        if (_dataService.UserChannel != null)
        {
            RegisterReceiver(_dataService.UserChannel);
        }

        _dataService.PropertyChanging += (_, args) =>
        {
            if (args.PropertyName != nameof(_dataService.UserChannel))
                return;

            if (_dataService.UserChannel != null)
                UnregisterReceiver(_dataService.UserChannel);
        };

        _dataService.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(_dataService.UserChannel))
                return;

            if (_dataService.UserChannel != null)
                RegisterReceiver(_dataService.UserChannel);
        };

        await Task.CompletedTask;
    }

    private void OnStreamDown(object? sender, OnStreamDownArgs args)
    {
        var listeners = _targets
            .Where(x => x.Key == PubSubType.StreamDown)
            .SelectMany(x => x.Value)
            .Where(x => x.ChannelId == args.ChannelId)
            .ToList();

        foreach (var listener in listeners)
        {
            if (listener.Listener == null || listener.Instance == null)
                continue;

            try
            {
                if (listener.Listener.SetValue(listener.Instance, false) == false)
                {
                    Console.WriteLine(
                        $"[ERROR][{nameof(TwitchPubSubService)}] Failed to set {listener.Instance.GetType().Name}.{listener.Listener.Name} to true");
                }
                else
                {
                    Console.WriteLine(
                        $"[DEBUG][{nameof(TwitchPubSubService)}] Setting {listener.Instance.GetType().Name}.{listener.Listener.Name} to true");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"[ERROR][{nameof(TwitchPubSubService)}] Exception while setting {listener.Instance?.GetType().Name}.{listener.Listener?.Name}: {e.Message}");
            }
        }
    }

    private void OnStreamUp(object? sender, OnStreamUpArgs args)
    {
        var listeners = _targets
            .Where(x => x.Key == PubSubType.StreamUp)
            .SelectMany(x => x.Value)
            .Where(x => x.ChannelId == args.ChannelId)
            .ToList();

        foreach (var listener in listeners)
        {
            if (listener.Listener == null || listener.Instance == null)
                continue;

            try
            {
                if (listener.Listener.SetValue(listener.Instance, true) == false)
                {
                    Console.WriteLine(
                        $"[ERROR][{nameof(TwitchPubSubService)}] Failed to set {listener.Instance.GetType().Name}.{listener.Listener.Name} to true");
                }
                else
                {
                    Console.WriteLine(
                        $"[DEBUG][{nameof(TwitchPubSubService)}] Setting {listener.Instance.GetType().Name}.{listener.Listener.Name} to true");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"[ERROR][{nameof(TwitchPubSubService)}] Exception while setting {listener.Instance?.GetType().Name}.{listener.Listener?.Name}: {e.Message}");
            }
        }
    }

    private void OnSubOnOnViewCount(object? sender, OnViewCountArgs args)
    {
        var listeners = _targets
            .Where(x => x.Key == PubSubType.VideoPlayback)
            .SelectMany(x => x.Value)
            .Where(x => x.ChannelId == args.ChannelId)
            .ToList();

        foreach (var listener in listeners)
        {
            if (listener.Listener == null || listener.Instance == null)
                continue;

            try
            {
                if (listener.Listener.SetValue(listener.Instance, args.Viewers) == false)
                {
                    Console.WriteLine(
                        $"[ERROR][{nameof(TwitchPubSubService)}] Failed to set {listener.Instance.GetType().Name}.{listener.Listener.Name} to {args.Viewers}");
                }
                else
                {
                    Console.WriteLine(
                        $"[DEBUG][{nameof(TwitchPubSubService)}] Setting {listener.Instance.GetType().Name}.{listener.Listener.Name} to {args.Viewers}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"[ERROR][{nameof(TwitchPubSubService)}] Exception while setting {listener.Instance?.GetType().Name}.{listener.Listener?.Name}: {e.Message}");
            }
        }
    }

    private void OnSubOnOnListenResponse(object? sender, OnListenResponseArgs args)
    {
        Console.WriteLine($"Listen Response: {args.Topic}");
    }

    private void OnSubOnOnPubSubServiceClosed(object? sender, EventArgs args)
    {
        Console.WriteLine("PubSub Closed");
    }

    private void OnSubOnOnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs args)
    {
        Console.WriteLine($"PubSub Error: {args.Exception.Message}");
    }

    private void OnSubOnOnPubSubServiceConnected(object? sender, EventArgs args)
    {
        Console.WriteLine("Connected to PubSub");
    }

    public void RegisterReceiver<T>(T receiver) where T : class
    {
        ArgumentNullException.ThrowIfNull(receiver, nameof(receiver));

        if (_sub == null)
        {
            Console.WriteLine($"[ERROR][{nameof(TwitchPubSubService)}] PubSub is not initialized");
            return;
        }

        Console.WriteLine($"[DEBUG][{nameof(TwitchPubSubService)}] Registering {receiver.GetType().Name}");

        var type = typeof(T);
        var publicTargets = type
            .GetProperties()
            .Concat(
                type.GetFields() as MemberInfo[]
            );

        foreach (var target in publicTargets)
        {
            if (target.GetCustomAttributes<PubSubAttribute>() is not { } attrs)
            {
                continue;
            }

            foreach (var attr in attrs)
            {
                var channelId =
                    type.GetProperty(attr.ChannelIdField)?.GetValue(receiver)?.ToString() ??
                    type.GetField(attr.ChannelIdField)?.GetValue(receiver)?.ToString();

                if (channelId == null)
                {
                    Console.WriteLine(
                        $"[ERROR][{nameof(TwitchPubSubService)}] {target.Name} is missing ChannelIdField named {attr.ChannelIdField}");
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(channelId))
                {
                    Console.WriteLine(
                        $"[ERROR][{nameof(TwitchPubSubService)}] {target.Name} ChannelIdField named {attr.ChannelIdField} is empty");
                    continue;
                }
                
                Console.WriteLine($"[DEBUG][{nameof(TwitchPubSubService)}] Registering {target.Name} for {attr.Type}");
                if (_targets.TryGetValue(attr.Type, out var listeners))
                {
                    listeners.Add(new PubSubListener
                    {
                        ChannelId = channelId,
                        Instance = receiver,
                        Listener = target
                    });
                }
                else
                {
                    _targets.Add(attr.Type, [
                        new PubSubListener
                        {
                            ChannelId = channelId,
                            Instance = receiver,
                            Listener = target
                        }
                    ]);
                }

                _sub.ListenToVideoPlayback(channelId);
                _sub.SendTopics(_dataService.AccessToken, true);
            }
        }
    }

    public void UnregisterReceiver<T>(T receiver) where T : class
    {
        ArgumentNullException.ThrowIfNull(receiver, nameof(receiver));

        if (_sub == null)
        {
            Console.WriteLine($"[ERROR][{nameof(TwitchPubSubService)}] PubSub is not initialized");
            return;
        }

        foreach (var (topic, listeners) in _targets)
        {
            var listener = listeners.Where(x => x.Instance == receiver).ToList();

            foreach (var l in listener)
            {
                _sub.ListenToVideoPlayback(l.ChannelId);
                _sub.SendTopics(_dataService.AccessToken, true);
            }

            _targets[topic].RemoveAll(x => x.Instance == receiver);
        }
    }
}