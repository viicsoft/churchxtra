using System.Collections.Generic;

namespace ChurchAI.Core.Entities;

public class Translation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string Language { get; set; } = "en";

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
