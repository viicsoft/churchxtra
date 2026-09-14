using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using ChurchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchAI.Infrastructure.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly BibleDbContext _context;

    public MediaRepository(BibleDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MediaItem>> GetAllMediaItemsAsync()
    {
        return await _context.MediaItems.OrderByDescending(m => m.DateAdded).ToListAsync();
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByTypeAsync(MediaType type)
    {
        return await _context.MediaItems
            .Where(m => m.Type == type)
            .OrderByDescending(m => m.DateAdded)
            .ToListAsync();
    }

    public async Task<MediaItem?> GetMediaItemByIdAsync(int id)
    {
        return await _context.MediaItems.FindAsync(id);
    }

    public async Task<MediaItem> AddMediaItemAsync(MediaItem item)
    {
        _context.MediaItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task DeleteMediaItemAsync(int id)
    {
        var item = await _context.MediaItems.FindAsync(id);
        if (item != null)
        {
            _context.MediaItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
