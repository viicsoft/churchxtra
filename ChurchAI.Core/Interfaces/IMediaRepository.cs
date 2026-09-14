using ChurchAI.Core.Entities;

namespace ChurchAI.Core.Interfaces;

public interface IMediaRepository
{
    Task<IEnumerable<MediaItem>> GetAllMediaItemsAsync();
    Task<IEnumerable<MediaItem>> GetMediaItemsByTypeAsync(MediaType type);
    Task<MediaItem?> GetMediaItemByIdAsync(int id);
    Task<MediaItem> AddMediaItemAsync(MediaItem item);
    Task DeleteMediaItemAsync(int id);
}
