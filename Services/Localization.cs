﻿using System.Text.Json;

namespace Servers.Services;

public class Localization
{
    private readonly Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);

    public Localization(string langDirectory, string defaultLanguage = "en")
    {
        var file = Path.Combine(langDirectory, $"{defaultLanguage}.json");
        if (File.Exists(file))
            Load(file);
    }

    private void Load(string file)
    {
        var json = File.ReadAllText(file);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        foreach (var kv in data)
            _strings[kv.Key] = kv.Value;
    }

    public string this[string key] => _strings.TryGetValue(key, out var v) ? v : key;
}