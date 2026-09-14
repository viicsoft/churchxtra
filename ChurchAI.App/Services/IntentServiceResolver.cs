using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Interfaces;
using ChurchAI.AI.Services;

namespace ChurchAI.App.Services;

public class IntentServiceResolver : IAIIntentService
{
    private readonly ISettingsService _settings;
    private readonly OllamaIntentService _localService;
    private readonly CloudIntentService _cloudService;
    private readonly string _cacheFilePath;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IntentServiceResolver(ISettingsService settings, OllamaIntentService localService, CloudIntentService cloudService)
    {
        _settings = settings;
        _localService = localService;
        _cloudService = cloudService;

        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChurchAI");
        Directory.CreateDirectory(appDataFolder);
        _cacheFilePath = Path.Combine(appDataFolder, "intent_cache.json");

        LoadCache();
    }

    private void LoadCache()
    {
        if (File.Exists(_cacheFilePath))
        {
            try
            {
                var json = File.ReadAllText(_cacheFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _cache[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load intent cache: {ex.Message}");
            }
        }
    }

    private async Task SaveCacheAsync()
    {
        try
        {
            var dict = new Dictionary<string, string>(_cache);
            var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save intent cache: {ex.Message}");
        }
    }

    public async Task<string?> ExtractBibleReferenceIntentAsync(string spokenText)
    {
        if (string.IsNullOrWhiteSpace(spokenText)) return null;

        var normalizedKey = new string(spokenText.ToLowerInvariant().Where(c => !char.IsPunctuation(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(normalizedKey)) return null;

        if (_cache.TryGetValue(normalizedKey, out var cachedRef))
        {
            return cachedRef == "NONE" ? null : cachedRef;
        }

        string? result = null;
        if (_settings.UseCloudServices)
        {
            result = await _cloudService.ExtractBibleReferenceIntentAsync(spokenText);
        }
        else
        {
            result = await _localService.ExtractBibleReferenceIntentAsync(spokenText);
        }

        // Cache the result if it is not an error message
        if (result == null || (!result.StartsWith("ERROR:") && !result.StartsWith("PARSE_ERROR:")))
        {
            _cache[normalizedKey] = result ?? "NONE";
            _ = SaveCacheAsync();
        }

        return result;
    }
}
