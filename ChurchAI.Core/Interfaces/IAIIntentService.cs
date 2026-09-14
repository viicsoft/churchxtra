using System.Threading.Tasks;

namespace ChurchAI.Core.Interfaces;

public interface IAIIntentService
{
    Task<string?> ExtractBibleReferenceIntentAsync(string spokenText);
}
