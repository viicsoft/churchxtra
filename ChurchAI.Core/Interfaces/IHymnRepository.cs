using System.Collections.Generic;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;

namespace ChurchAI.Core.Interfaces;

public interface IHymnRepository
{
    Task<Hymn?> GetHymnByNumberAsync(int number, string? bookName = null);
    Task<Hymn?> MatchHymnByLyricsAsync(string spokenText, string? bookName = null);
    Task<List<Hymn>> GetAllHymnsAsync(string? bookName = null);
    Task<List<string>> GetAvailableHymnBooksAsync();
    Task SaveHymnBookAsync(string bookName, List<Hymn> hymns);
}
