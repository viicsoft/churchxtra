using System.Collections.Generic;

namespace ChurchAI.Core.Entities;

public class Book
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BookNumber { get; set; }
    
    public int TranslationId { get; set; }
    public Translation? Translation { get; set; }

    public ICollection<Verse> Verses { get; set; } = new List<Verse>();
}
