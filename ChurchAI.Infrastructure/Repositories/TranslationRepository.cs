using System.Collections.Generic;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using ChurchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchAI.Infrastructure.Repositories;

public class TranslationRepository : ITranslationRepository
{
    private readonly BibleDbContext _context;

    public TranslationRepository(BibleDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Translation>> GetAllTranslationsAsync()
    {
        return await _context.Translations.ToListAsync();
    }

    public async Task<Translation?> GetTranslationAsync(int id)
    {
        return await _context.Translations.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Translation?> GetTranslationByAbbreviationAsync(string abbreviation)
    {
        return await _context.Translations.FirstOrDefaultAsync(t => t.Abbreviation == abbreviation);
    }

    public async Task InitializeDatabaseAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task SeedBibleAsync()
    {
        // Check if data exists
        if (await _context.Translations.AnyAsync())
            return;

        // Seed a basic translation placeholder
        var kjv = new Translation 
        { 
            Name = "King James Version", 
            Abbreviation = "KJV", 
            Language = "en" 
        };
        _context.Translations.Add(kjv);
        await _context.SaveChangesAsync();

        // Further seeding would involve reading from the KJV SQLite database 
        // that will be imported later as per requirements.
    }
}
