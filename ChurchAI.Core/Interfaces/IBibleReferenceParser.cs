using ChurchAI.Core.Models;

namespace ChurchAI.Core.Interfaces;

public interface IBibleReferenceParser
{
    BibleReference? Parse(string spokenText);
    BibleReference? ParseWithContext(string spokenText);
    void SetPendingBook(string bookName);
    string? GetPendingBook();
    void ClearPendingBook();
}
