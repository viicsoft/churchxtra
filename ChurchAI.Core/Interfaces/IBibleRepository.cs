using System.Collections.Generic;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;

namespace ChurchAI.Core.Interfaces;

public interface IBibleRepository
{
    Task<Verse?> GetVerseAsync(int translationId, string bookName, int chapter, int verseNumber);
    Task<IEnumerable<Verse>> GetChapterAsync(int translationId, string bookName, int chapter);
    Task<IEnumerable<Verse>> SearchAsync(int translationId, string query);
    Task<IEnumerable<Verse>> SearchAllTranslationsAsync(string query);
    Task<IEnumerable<Verse>> SearchBookAsync(int translationId, string bookName, string query);
    Task<IEnumerable<Verse>> GetVerseInAllTranslationsAsync(string bookName, int chapter, int verseNumber);
    Task<IEnumerable<Verse>> GetSucceedingVersesAsync(int bookId, int startChapter, int startVerseNumber, int limit);
    Task<int> GetTranslationIdAsync(string abbreviation);
    Task<IEnumerable<Translation>> GetAvailableTranslationsAsync();
    Task<IDictionary<string, string>> GetAllSpokenPhrasesAsync();
    Task InitializeDatabaseAsync();
}
