using System.Threading.Tasks;
using ChurchAI.Core.Entities;

namespace ChurchAI.App.Services.Interfaces;

public interface IAITemplateExtractorService
{
    Task<CustomTemplate> ExtractTemplateFromImageAsync(string imageFilePath, CustomTemplateCategory preferredCategory);
}
