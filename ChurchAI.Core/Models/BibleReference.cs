namespace ChurchAI.Core.Models;

public class BibleReference
{
    public string BookName { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public int? Verse { get; set; }

    public override string ToString()
    {
        if (Verse.HasValue)
        {
            return $"{BookName} {Chapter}:{Verse.Value}";
        }
        return $"{BookName} {Chapter}";
    }
}
