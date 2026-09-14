using System.Collections.Generic;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using ChurchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchAI.Infrastructure.Repositories;

public class LowerThirdRepository : ILowerThirdRepository
{
    private readonly BibleDbContext _context;

    public LowerThirdRepository(BibleDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LowerThirdItem>> GetAllLowerThirdsAsync()
    {
        return await _context.LowerThirds
            .OrderByDescending(l => l.DateCreated)
            .ToListAsync();
    }

    public async Task<LowerThirdItem?> GetLowerThirdByIdAsync(int id)
    {
        return await _context.LowerThirds.FindAsync(id);
    }

    public async Task<LowerThirdItem> AddLowerThirdAsync(LowerThirdItem item)
    {
        _context.LowerThirds.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateLowerThirdAsync(LowerThirdItem item)
    {
        _context.LowerThirds.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteLowerThirdAsync(int id)
    {
        var item = await _context.LowerThirds.FindAsync(id);
        if (item != null)
        {
            _context.LowerThirds.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
