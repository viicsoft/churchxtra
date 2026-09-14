using System.Collections.Generic;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;

namespace ChurchAI.Core.Interfaces;

public interface ILowerThirdRepository
{
    Task<IEnumerable<LowerThirdItem>> GetAllLowerThirdsAsync();
    Task<LowerThirdItem?> GetLowerThirdByIdAsync(int id);
    Task<LowerThirdItem> AddLowerThirdAsync(LowerThirdItem item);
    Task UpdateLowerThirdAsync(LowerThirdItem item);
    Task DeleteLowerThirdAsync(int id);
}
