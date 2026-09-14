namespace ChurchAI.Core.Entities;

public class Verse
{
    public int Id { get; set; }
    public int Chapter { get; set; }
    public int VerseNumber { get; set; }
    public string Text { get; set; } = string.Empty;

    public int BookId { get; set; }
    public Book? Book { get; set; }

    public string DisplayReference => Book != null ? $"{Book.Name} {Chapter}:{VerseNumber}" : $"{Chapter}:{VerseNumber}";
}
