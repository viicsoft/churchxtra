using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using ChurchAI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchAI.Infrastructure.Repositories;

public class HymnRepository : IHymnRepository
{
    private readonly BibleDbContext _context;

    public HymnRepository(BibleDbContext context)
    {
        _context = context;
    }

    public async Task<Hymn?> GetHymnByNumberAsync(int number, string? bookName = null)
    {
        bookName ??= "Default";
        return await _context.Set<Hymn>()
            .FirstOrDefaultAsync(h => h.Number == number && h.Book == bookName);
    }

    public async Task<List<Hymn>> GetAllHymnsAsync(string? bookName = null)
    {
        bookName ??= "Default";
        return await _context.Set<Hymn>()
            .Where(h => h.Book == bookName)
            .OrderBy(h => h.Number)
            .ToListAsync();
    }

    public async Task<List<string>> GetAvailableHymnBooksAsync()
    {
        var books = await _context.Set<Hymn>()
            .Select(h => h.Book)
            .Distinct()
            .ToListAsync();
        
        if (!books.Contains("Default"))
        {
            books.Insert(0, "Default");
        }
        return books;
    }

    public async Task SaveHymnBookAsync(string bookName, List<Hymn> hymns)
    {
        if (string.IsNullOrWhiteSpace(bookName)) return;

        // Delete existing hymns with this book name to prevent duplicate entries on overwrite
        var existing = await _context.Set<Hymn>().Where(h => h.Book == bookName).ToListAsync();
        if (existing.Any())
        {
            _context.Set<Hymn>().RemoveRange(existing);
        }

        // Set the Book identifier
        foreach (var hymn in hymns)
        {
            hymn.Book = bookName;
        }

        await _context.Set<Hymn>().AddRangeAsync(hymns);
        await _context.SaveChangesAsync();
    }

    public async Task<Hymn?> MatchHymnByLyricsAsync(string spokenText, string? bookName = null)
    {
        if (string.IsNullOrWhiteSpace(spokenText)) return null;

        bookName ??= "Default";
        var normalizedInput = new string(spokenText.ToLowerInvariant().Where(c => !char.IsPunctuation(c)).ToArray()).Trim();
        if (normalizedInput.Length < 10) return null; // Too short to match reliably

        var hymns = await _context.Set<Hymn>().Where(h => h.Book == bookName).ToListAsync();
        foreach (var hymn in hymns)
        {
            var normalizedLyrics = new string(hymn.Lyrics.ToLowerInvariant().Where(c => !char.IsPunctuation(c)).ToArray()).Trim();

            // 1. Direct check
            if (normalizedLyrics.Contains(normalizedInput) || normalizedInput.Contains(hymn.Title.ToLowerInvariant()))
            {
                return hymn;
            }

            // 2. Sliding word window check (4 consecutive words)
            var words = normalizedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i <= words.Length - 4; i++)
            {
                var phrase = string.Join(" ", words.Skip(i).Take(4));
                if (normalizedLyrics.Contains(phrase))
                {
                    return hymn;
                }
            }
        }

        return null;
    }
}
