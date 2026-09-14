using System.Collections.Generic;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;

namespace ChurchAI.App.Services.Interfaces;

public interface ICustomTemplateService
{
    Task<List<CustomTemplate>> GetTemplatesAsync();
    Task<List<CustomTemplate>> GetTemplatesByCategoryAsync(CustomTemplateCategory category);
    Task<CustomTemplate?> GetTemplateByIdAsync(string id);
    Task SaveTemplateAsync(CustomTemplate template);
    Task DeleteTemplateAsync(string id);
    Task<CustomTemplate> DuplicateTemplateAsync(string id);
}
