using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;

namespace ChurchAI.App.Services;

public class CustomTemplateService : ICustomTemplateService
{
    private readonly string _storageDir;
    private readonly JsonSerializerOptions _jsonOptions;

    public CustomTemplateService()
    {
        _storageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChurchAI", "CustomTemplates");
        if (!Directory.Exists(_storageDir))
        {
            Directory.CreateDirectory(_storageDir);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<CustomTemplate>> GetTemplatesAsync()
    {
        var templates = new List<CustomTemplate>();
        try
        {
            var files = Directory.GetFiles(_storageDir, "*.json");
            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                var t = JsonSerializer.Deserialize<CustomTemplate>(json, _jsonOptions);
                if (t != null)
                {
                    templates.Add(t);
                }
            }
        }
        catch { }

        return templates.OrderByDescending(x => x.DateModified).ToList();
    }

    public async Task<List<CustomTemplate>> GetTemplatesByCategoryAsync(CustomTemplateCategory category)
    {
        var all = await GetTemplatesAsync();
        return all.Where(x => x.Category == category).ToList();
    }

    public async Task<CustomTemplate?> GetTemplateByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var filePath = Path.Combine(_storageDir, $"{id}.json");
        if (!File.Exists(filePath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<CustomTemplate>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveTemplateAsync(CustomTemplate template)
    {
        if (template == null) return;
        if (string.IsNullOrWhiteSpace(template.Id))
        {
            template.Id = Guid.NewGuid().ToString();
        }
        template.DateModified = DateTime.UtcNow;

        var filePath = Path.Combine(_storageDir, $"{template.Id}.json");
        var json = JsonSerializer.Serialize(template, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public Task DeleteTemplateAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return Task.CompletedTask;
        var filePath = Path.Combine(_storageDir, $"{id}.json");
        if (File.Exists(filePath))
        {
            try { File.Delete(filePath); } catch { }
        }
        return Task.CompletedTask;
    }

    public async Task<CustomTemplate> DuplicateTemplateAsync(string id)
    {
        var original = await GetTemplateByIdAsync(id);
        if (original == null)
        {
            throw new InvalidOperationException($"Template {id} not found.");
        }

        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var copy = JsonSerializer.Deserialize<CustomTemplate>(json, _jsonOptions)!;
        copy.Id = Guid.NewGuid().ToString();
        copy.Name = $"{original.Name} (Copy)";
        copy.DateCreated = DateTime.UtcNow;
        copy.DateModified = DateTime.UtcNow;

        await SaveTemplateAsync(copy);
        return copy;
    }
}
