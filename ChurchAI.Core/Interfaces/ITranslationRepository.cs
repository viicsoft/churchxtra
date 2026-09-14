using System.Collections.Generic;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;

namespace ChurchAI.Core.Interfaces;

public interface ITranslationRepository
{
    Task<IEnumerable<Translation>> GetAllTranslationsAsync();
    Task<Translation?> GetTranslationAsync(int id);
    Task<Translation?> GetTranslationByAbbreviationAsync(string abbreviation);
    
    Task InitializeDatabaseAsync();
    Task SeedBibleAsync();
}
