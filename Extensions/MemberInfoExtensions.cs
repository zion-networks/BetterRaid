using System.Reflection;

namespace BetterRaid.Extensions;

public static class MemberInfoExtensions
{
    public static bool SetValue<T>(this MemberInfo member, object instance, T value)
    {
        var targetType = member switch
        {
            PropertyInfo p => p.PropertyType,
            FieldInfo f => f.FieldType,
            _ => null
        };
        
        if (targetType == null)
            return false;
        
        if (member is PropertyInfo property)
        {
            if (targetType == typeof(T) || targetType.IsAssignableFrom(typeof(T)))
            {
                property.SetValue(instance, value);
                return true;
            }
            
            if (targetType == typeof(string))
            {
                property.SetValue(instance, value?.ToString());
                return true;
            }
            
            return false;
        }

        if (member is FieldInfo field)
        {
            if (targetType == typeof(T) || targetType.IsAssignableFrom(typeof(T)))
            {
                field.SetValue(instance, value);
                return true;
            }
            
            if (targetType == typeof(string))
            {
                field.SetValue(instance, value?.ToString());
                return true;
            }
            
            return false;
        }

        return false;
    }
}