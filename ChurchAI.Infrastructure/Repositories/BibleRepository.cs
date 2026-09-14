using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using ChurchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchAI.Infrastructure.Repositories;

public class BibleRepository : IBibleRepository
{
    private readonly BibleDbContext _context;
    private readonly DatabaseSeeder _seeder;
    private static readonly System.Threading.SemaphoreSlim _dbLock = new(1, 1);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> TranslationIdCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, List<int>> BookIdsCache = new(StringComparer.OrdinalIgnoreCase);

    public BibleRepository(BibleDbContext context, Microsoft.Extensions.Logging.ILogger<DatabaseSeeder> seederLogger)
    {
        _context = context;
        _seeder = new DatabaseSeeder(context, seederLogger);
    }

    public async Task InitializeDatabaseAsync()
    {
        await _context.Database.EnsureCreatedAsync();
        
        // Dynamically ensure Hymns table exists if the database already existed
        await _context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS \"Hymns\" (" +
            "\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"Number\" INTEGER NOT NULL, " +
            "\"Title\" TEXT NOT NULL, " +
            "\"Lyrics\" TEXT NOT NULL, " +
            "\"Book\" TEXT NOT NULL DEFAULT 'Default');"
        );

        // Ensure the Book column is added to the database table if it was created previously
        try
        {
            await _context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Hymns\" ADD COLUMN \"Book\" TEXT NOT NULL DEFAULT 'Default';");
        }
        catch
        {
            // Ignore if the column already exists
        }

        // Ensure SpokenPhrases table and index exist
        await _context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS \"SpokenPhrases\" (" +
            "\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"NormalizedPhrase\" TEXT NOT NULL, " +
            "\"ReferenceKey\" TEXT NOT NULL);" +
            "CREATE INDEX IF NOT EXISTS \"IX_SpokenPhrases_NormalizedPhrase\" ON \"SpokenPhrases\" (\"NormalizedPhrase\");"
        );

        // Ensure MediaItems table exists
        await _context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS \"MediaItems\" (" +
            "\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"Name\" TEXT NOT NULL, " +
            "\"FilePath\" TEXT NOT NULL, " +
            "\"Type\" INTEGER NOT NULL, " +
            "\"Category\" TEXT NOT NULL DEFAULT 'General', " +
            "\"IsPreset\" INTEGER NOT NULL DEFAULT 0, " +
            "\"ThumbnailPath\" TEXT NULL, " +
            "\"DateAdded\" TEXT NOT NULL);"
        );

        // Ensure LowerThirds table exists
        await _context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS \"LowerThirds\" (" +
            "\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"Name\" TEXT NOT NULL, " +
            "\"TagText\" TEXT NOT NULL, " +
            "\"Title\" TEXT NOT NULL, " +
            "\"Subtitle\" TEXT NOT NULL, " +
            "\"TagBgColorHex\" TEXT NOT NULL DEFAULT '', " +
            "\"TagTextColorHex\" TEXT NOT NULL DEFAULT '', " +
            "\"TitleBgColorHex\" TEXT NOT NULL DEFAULT '', " +
            "\"TitleTextColorHex\" TEXT NOT NULL DEFAULT '', " +
            "\"SubtitleBgColorHex\" TEXT NOT NULL DEFAULT '', " +
            "\"SubtitleTextColorHex\" TEXT NOT NULL DEFAULT '', " +
            "\"StylePreset\" INTEGER NOT NULL DEFAULT 0, " +
            "\"Position\" INTEGER NOT NULL DEFAULT 0, " +
            "\"IsPreset\" INTEGER NOT NULL DEFAULT 0, " +
            "\"DateCreated\" TEXT NOT NULL);"
        );

        string[] lowerThirdCols = new[]
        {
            "ALTER TABLE \"LowerThirds\" ADD COLUMN \"TagBgColorHex\" TEXT NOT NULL DEFAULT '';",
            "ALTER TABLE \"LowerThirds\" ADD COLUMN \"TagTextColorHex\" TEXT NOT NULL DEFAULT '';",
            "ALTER TABLE \"LowerThirds\" ADD COLUMN \"TitleBgColorHex\" TEXT NOT NULL DEFAULT '';",
            "ALTER TABLE \"LowerThirds\" ADD COLUMN \"TitleTextColorHex\" TEXT NOT NULL DEFAULT '';",
            "ALTER TABLE \"LowerThirds\" ADD COLUMN \"SubtitleBgColorHex\" TEXT NOT NULL DEFAULT '';",
            "ALTER TABLE \"LowerThirds\" ADD COLUMN \"SubtitleTextColorHex\" TEXT NOT NULL DEFAULT '';"
        };
        foreach (var colSql in lowerThirdCols)
        {
            try { await _context.Database.ExecuteSqlRawAsync(colSql); } catch { }
        }

        await _seeder.SeedBibleAsync();
        await _seeder.SeedHymnsAsync();
        await _seeder.SeedSpokenPhrasesAsync();
    }

    public async Task<Verse?> GetVerseAsync(int translationId, string bookName, int chapter, int verseNumber)
    {
        await _dbLock.WaitAsync();
        try
        {
            return await _context.Verses
                .Include(v => v.Book)
                .FirstOrDefaultAsync(v => v.Book!.TranslationId == translationId && 
                                          v.Book.Name.ToLower() == bookName.ToLower() && 
                                          v.Chapter == chapter && 
                                          v.VerseNumber == verseNumber);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IEnumerable<Verse>> GetChapterAsync(int translationId, string bookName, int chapter)
    {
        await _dbLock.WaitAsync();
        try
        {
            return await _context.Verses
                .Include(v => v.Book)
                .Where(v => v.Book!.TranslationId == translationId && 
                            v.Book.Name.ToLower() == bookName.ToLower() && 
                            v.Chapter == chapter)
                .OrderBy(v => v.VerseNumber)
                .ToListAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IEnumerable<Verse>> SearchAsync(int translationId, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Verse>();

        await _dbLock.WaitAsync();
        try
        {
            return await _context.Verses
                .Include(v => v.Book)
                .Where(v => v.Book!.TranslationId == translationId && EF.Functions.Like(v.Text, $"%{query}%"))
                .OrderBy(v => v.Book!.BookNumber)
                .ThenBy(v => v.Chapter)
                .ThenBy(v => v.VerseNumber)
                .ToListAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IEnumerable<Verse>> SearchAllTranslationsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Verse>();

        await _dbLock.WaitAsync();
        try
        {
            return await _context.Verses
                .Include(v => v.Book)
                .ThenInclude(b => b!.Translation)
                .Where(v => EF.Functions.Like(v.Text, $"%{query}%"))
                .OrderBy(v => v.Book!.BookNumber)
                .ThenBy(v => v.Chapter)
                .ThenBy(v => v.VerseNumber)
                .ThenBy(v => v.Book!.TranslationId)
                .Take(100)
                .ToListAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IEnumerable<Verse>> SearchBookAsync(int translationId, string bookName, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Verse>();

        await _dbLock.WaitAsync();
        try
        {
            return await _context.Verses
                .Include(v => v.Book)
                .Where(v => v.Book!.TranslationId == translationId && 
                            v.Book.Name.ToLower() == bookName.ToLower() && 
                            EF.Functions.Like(v.Text, $"%{query}%"))
                .OrderBy(v => v.Chapter)
                .ThenBy(v => v.VerseNumber)
                .ToListAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IEnumerable<Verse>> GetVerseInAllTranslationsAsync(string bookName, int chapter, int verseNumber)
    {
        await _dbLock.WaitAsync();
        try
        {
            if (!BookIdsCache.TryGetValue(bookName, out List<int>? bookIds))
            {
                bookIds = await _context.Books
                    .Where(b => b.Name.ToLower() == bookName.ToLower())
                    .Select(b => b.Id)
                    .ToListAsync();

                if (bookIds != null && bookIds.Any())
                {
                    BookIdsCache[bookName] = bookIds;
                }
            }

            if (bookIds == null || !bookIds.Any())
                return new List<Verse>();

            return await _context.Verses
                .Include(v => v.Book)
                .ThenInclude(b => b!.Translation)
                .Where(v => bookIds.Contains(v.BookId) && 
                            v.Chapter == chapter && 
                            v.VerseNumber == verseNumber)
                .OrderBy(v => v.Book!.TranslationId)
                .ToListAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IEnumerable<Verse>> GetSucceedingVersesAsync(int bookId, int startChapter, int startVerseNumber, int limit)
    {
        await _dbLock.WaitAsync();
        try
        {
            return await _context.Verses
                .Include(v => v.Book)
                .Where(v => v.BookId == bookId && 
                            (v.Chapter > startChapter || (v.Chapter == startChapter && v.VerseNumber > startVerseNumber)))
                .OrderBy(v => v.Chapter)
                .ThenBy(v => v.VerseNumber)
                .Take(limit)
                .ToListAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<int> GetTranslationIdAsync(string abbreviation)
    {
        if (string.IsNullOrWhiteSpace(abbreviation)) return 1;
        if (TranslationIdCache.TryGetValue(abbreviation, out int cachedId))
        {
            return cachedId;
        }

        await _dbLock.WaitAsync();
        try
        {
            var translation = await _context.Translations
                .FirstOrDefaultAsync(t => t.Abbreviation.ToUpper() == abbreviation.ToUpper());
            int id = translation?.Id ?? 1;
            TranslationIdCache[abbreviation] = id;
            return id;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IEnumerable<Translation>> GetAvailableTranslationsAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            return await _context.Translations.OrderBy(t => t.Id).ToListAsync();
        }
        finally
        {
            _dbLock.Release();
        }
    }

    public async Task<IDictionary<string, string>> GetAllSpokenPhrasesAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            return await _context.SpokenPhrases
                .ToDictionaryAsync(sp => sp.NormalizedPhrase, sp => sp.ReferenceKey, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _dbLock.Release();
        }
    }
}
