namespace ChurchAI.Core.Entities;

public class SpokenPhrase
{
    public int Id { get; set; }
    public string NormalizedPhrase { get; set; } = string.Empty;
    public string ReferenceKey { get; set; } = string.Empty;
}
