using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChurchAI.App.Services.Interfaces;

public interface ISearchHistoryService
{
    Task<IEnumerable<string>> GetSearchHistoryAsync();
    Task AddSearchAsync(string query);
    Task ClearHistoryAsync();
}
