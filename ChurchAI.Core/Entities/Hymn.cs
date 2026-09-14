namespace ChurchAI.Core.Entities;

public class Hymn
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Lyrics { get; set; } = string.Empty;
    public string Book { get; set; } = "Default";
}
