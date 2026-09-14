using ChurchAI.Core.Entities;

namespace ChurchAI.App.Services.Interfaces;

public interface IMediaService
{
    Task<IEnumerable<MediaItem>> GetMediaItemsAsync();
    Task<MediaItem> ImportMediaAsync(string sourceFilePath, string category = "Custom");
    Task<MediaItem> AddMediaAsync(MediaItem item);
    Task DeleteMediaAsync(int id);
    Task EnsurePresetMediaAsync();
    Task<MediaItem?> GetActiveBackgroundAsync();
}
