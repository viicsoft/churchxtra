using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;

namespace ChurchAI.App.Services;

public class SearchHistoryService : ISearchHistoryService
{
    private readonly string _filePath;
    private const int MaxHistory = 20;
    private List<string> _cache = new();

    public SearchHistoryService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "ChurchXtra");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "search_history.json");
    }

    public async Task<IEnumerable<string>> GetSearchHistoryAsync()
    {
        if (_cache.Any()) return _cache;

        if (!File.Exists(_filePath)) return Enumerable.Empty<string>();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _cache = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            _cache = new List<string>();
        }

        return _cache;
    }

    public async Task AddSearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        await GetSearchHistoryAsync(); // Ensure cache is loaded

        _cache.Remove(query); // Remove if exists to move to top
        _cache.Insert(0, query);

        if (_cache.Count > MaxHistory)
        {
            _cache = _cache.Take(MaxHistory).ToList();
        }

        await SaveAsync();
    }

    public async Task ClearHistoryAsync()
    {
        _cache.Clear();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cache);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
            // Ignore write errors for now
        }
    }
}
