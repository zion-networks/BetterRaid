using System.Runtime.CompilerServices;
using BetterRaid.Models.Database;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace BetterRaid.Extensions;

public static class ReactiveObjectExtensions
{
    public static TRet RaiseAndSetIfChanged<TObj, TRet>(
        this TObj reactiveObject,
        ref TRet backingField,
        TRet newValue,
        BetterRaidDatabase? db,
        [CallerMemberName] string? propertyName = null) where TObj : IReactiveObject
    {
        reactiveObject.RaiseAndSetIfChanged(ref backingField, newValue, propertyName);

        if (string.IsNullOrEmpty(propertyName))
        {
            return newValue;
        }
        
        if (db == null)
        {
            return newValue;
        }
        
        var dbType = typeof(BetterRaidDatabase);
        if (dbType.GetProperty(propertyName) is { } property)
        {
            property.SetValue(db, newValue);
        }
        else
        {
            LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger(nameof(ReactiveObjectExtensions))
                .LogWarning("Property {PropertyName} not found in {DbType.Name}", propertyName, dbType);
        }
        
        return newValue;
    }
}