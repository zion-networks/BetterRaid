using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterRaid.Attributes;
using BetterRaid.Extensions;
using BetterRaid.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace BetterRaid.Services.Implementations;

public class TwitchPubSubService : ITwitchPubSubService
{
    private readonly Dictionary<PubSubType, List<PubSubListener>> _targets = new();
    private readonly TwitchPubSub _sub;
    private readonly ITwitchDataService _dataService;

    public TwitchPubSubService(ITwitchDataService dataService)
    {
        _dataService = dataService;
        
        _sub = new TwitchPubSub();
        
        _sub.OnPubSubServiceConnected += OnSubOnOnPubSubServiceConnected;
        _sub.OnPubSubServiceError += OnSubOnOnPubSubServiceError;
        _sub.OnPubSubServiceClosed += OnSubOnOnPubSubServiceClosed;
        _sub.OnListenResponse += OnSubOnOnListenResponse;
        _sub.OnViewCount += OnSubOnOnViewCount;
        
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
                    Console.WriteLine($"[ERROR][{nameof(TwitchPubSubService)}] Failed to set {listener.Instance.GetType().Name}.{listener.Listener.Name} to {args.Viewers}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG][{nameof(TwitchPubSubService)}] Setting {listener.Instance.GetType().Name}.{listener.Listener.Name} to {args.Viewers}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR][{nameof(TwitchPubSubService)}] Exception while setting {listener.Instance?.GetType().Name}.{listener.Listener?.Name}: {e.Message}");
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

        Console.WriteLine($"[DEBUG][{nameof(TwitchPubSubService)}] Registering {receiver.GetType().Name}");
        
        var type = typeof(T);
        var publicTargets = type
            .GetProperties()
            .Concat(
                type.GetFields() as MemberInfo[]
            );

        foreach (var target in publicTargets)
        {
            if (target.GetCustomAttribute<PubSubAttribute>() is not { } attr)
            {
                continue;
            }
            
            var channelId =
                type.GetProperty(attr.ChannelIdField)?.GetValue(receiver)?.ToString() ??
                type.GetField(attr.ChannelIdField)?.GetValue(receiver)?.ToString();
            
            if (string.IsNullOrEmpty(channelId))
            {
                Console.WriteLine($"[ERROR][{nameof(TwitchPubSubService)}] {target.Name} is missing ChannelIdField named {attr.ChannelIdField}");
                continue;
            }
            
            switch (attr.Type)
            {
                case PubSubType.Bits:
                    break;
                case PubSubType.ChannelPoints:
                    break;
                case PubSubType.Follows:
                    break;
                case PubSubType.Raids:
                    break;
                case PubSubType.Subscriptions:
                    break;
                case PubSubType.VideoPlayback:
                    Console.WriteLine($"[DEBUG][{nameof(TwitchPubSubService)}] Registering {target.Name} for {attr.Type}");
                    if (_targets.TryGetValue(PubSubType.VideoPlayback, out var value))
                    {
                        value.Add(new PubSubListener
                        {
                            ChannelId = channelId,
                            Instance = receiver,
                            Listener = target
                        });
                    }
                    else
                    {
                        _targets.Add(PubSubType.VideoPlayback, [
                            new PubSubListener
                            {
                                ChannelId = channelId,
                                Instance = receiver,
                                Listener = target
                            }
                        ]);
                    }
                    
                    _sub.ListenToVideoPlayback(channelId);
                    _sub.SendTopics(_dataService.AccessToken);
                    
                    break;
            }
        }
    }
    
    public void UnregisterReceiver<T>(T receiver) where T : class
    {
        ArgumentNullException.ThrowIfNull(receiver, nameof(receiver));
        
        foreach (var (topic, listeners) in _targets)
        {
            var listener = listeners.Where(x => x.Instance == receiver).ToList();

            foreach (var l in listener)
            {
                switch (topic)
                {
                    case PubSubType.Bits:
                        break;
                    case PubSubType.ChannelPoints:
                        break;
                    case PubSubType.Follows:
                        break;
                    case PubSubType.Raids:
                        break;
                    case PubSubType.Subscriptions:
                        break;
                    case PubSubType.VideoPlayback:
                        _sub.ListenToVideoPlayback(l.ChannelId);
                        _sub.SendTopics(_dataService.AccessToken, true);
                        break;
                }
            }
            
            _targets[topic].RemoveAll(x => x.Instance == receiver);
        }
    }
}