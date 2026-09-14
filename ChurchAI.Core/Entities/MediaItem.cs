namespace ChurchAI.Core.Entities;

public enum MediaType
{
    Image,
    Video,
    Color,
    YouTube
}

public class MediaItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public string Category { get; set; } = "General"; // Preset, Background, Motion, Video, Custom
    public bool IsPreset { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}
