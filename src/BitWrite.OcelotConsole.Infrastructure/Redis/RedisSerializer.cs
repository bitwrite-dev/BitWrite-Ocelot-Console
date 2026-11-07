using System.Text.Json;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

public static class RedisSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    public static HashEntry[] ToHashEntries(object obj)
    {
        var properties = obj.GetType().GetProperties();
        var entries = new List<HashEntry>();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            if (value != null)
            {
                var json = Serialize(value);
                entries.Add(new HashEntry(prop.Name, json));
            }
        }

        return entries.ToArray();
    }

    public static T FromHashEntries<T>(HashEntry[] entries) where T : new()
    {
        var obj = new T();
        var properties = typeof(T).GetProperties()
            .ToDictionary(p => p.Name, p => p);

        foreach (var entry in entries)
        {
            if (properties.TryGetValue(entry.Name!, out var prop))
            {
                var value = Deserialize(prop.PropertyType, entry.Value.ToString());
                prop.SetValue(obj, value);
            }
        }

        return obj;
    }

    private static object? Deserialize(Type type, string json)
    {
        return JsonSerializer.Deserialize(json, type, Options);
    }
}