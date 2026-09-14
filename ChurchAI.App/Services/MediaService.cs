using System.IO;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ChurchAI.App.Services;

public class MediaService : IMediaService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsService _settingsService;
    private readonly string _mediaFolder;

    public MediaService(IServiceProvider serviceProvider, ISettingsService settingsService)
    {
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;

        _mediaFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChurchAI",
            "Media"
        );

        Directory.CreateDirectory(_mediaFolder);
        Directory.CreateDirectory(Path.Combine(_mediaFolder, "Images"));
        Directory.CreateDirectory(Path.Combine(_mediaFolder, "Videos"));
        Directory.CreateDirectory(Path.Combine(_mediaFolder, "Presets"));
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsAsync()
    {
        await EnsurePresetMediaAsync();
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
        return await repo.GetAllMediaItemsAsync();
    }

    public async Task<MediaItem> ImportMediaAsync(string sourceFilePath, string category = "Custom")
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("Media file not found.", sourceFilePath);

        var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var isVideo = extension is ".mp4" or ".avi" or ".mov" or ".wmv" or ".mkv";
        var subDir = isVideo ? "Videos" : "Images";
        var destPath = Path.Combine(_mediaFolder, subDir, fileName);

        File.Copy(sourceFilePath, destPath, overwrite: true);

        var mediaItem = new MediaItem
        {
            Name = Path.GetFileNameWithoutExtension(sourceFilePath),
            FilePath = destPath,
            Type = isVideo ? MediaType.Video : MediaType.Image,
            Category = category,
            IsPreset = false,
            DateAdded = DateTime.UtcNow
        };

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
        return await repo.AddMediaItemAsync(mediaItem);
    }

    public async Task<MediaItem> AddMediaAsync(MediaItem item)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
        return await repo.AddMediaItemAsync(item);
    }

    public async Task DeleteMediaAsync(int id)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
        var item = await repo.GetMediaItemByIdAsync(id);
        if (item != null)
        {
            if (!item.IsPreset && File.Exists(item.FilePath))
            {
                try { File.Delete(item.FilePath); } catch { }
            }
            await repo.DeleteMediaItemAsync(id);
        }
    }

    public async Task<MediaItem?> GetActiveBackgroundAsync()
    {
        var activeId = _settingsService.ActiveBackgroundMediaId;
        if (activeId > 0)
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
            return await repo.GetMediaItemByIdAsync(activeId);
        }
        return null;
    }

    public async Task EnsurePresetMediaAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
        var existing = await repo.GetAllMediaItemsAsync();
        if (existing.Any()) return;

        // Seed Default Built-In Presets
        var presets = new List<MediaItem>
        {
            new MediaItem
            {
                Name = "Classic Black",
                FilePath = "#000000",
                Type = MediaType.Color,
                Category = "Preset",
                IsPreset = true
            },
            new MediaItem
            {
                Name = "Midnight Navy",
                FilePath = "#0B0F19",
                Type = MediaType.Color,
                Category = "Preset",
                IsPreset = true
            },
            new MediaItem
            {
                Name = "Deep Royal Blue",
                FilePath = "#0F172A",
                Type = MediaType.Color,
                Category = "Preset",
                IsPreset = true
            },
            new MediaItem
            {
                Name = "Dark Emerald",
                FilePath = "#064E3B",
                Type = MediaType.Color,
                Category = "Preset",
                IsPreset = true
            },
            new MediaItem
            {
                Name = "Imperial Purple",
                FilePath = "#3B0764",
                Type = MediaType.Color,
                Category = "Preset",
                IsPreset = true
            },
            new MediaItem
            {
                Name = "Rich Burgundy",
                FilePath = "#450A0A",
                Type = MediaType.Color,
                Category = "Preset",
                IsPreset = true
            }
        };

        foreach (var preset in presets)
        {
            await repo.AddMediaItemAsync(preset);
        }
    }
}
