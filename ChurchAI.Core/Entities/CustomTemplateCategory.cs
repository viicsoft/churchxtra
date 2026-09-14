namespace ChurchAI.Core.Entities;

public enum CustomTemplateCategory
{
    Speaker,
    Scripture,
    Hymn
}

public enum ShapeType
{
    Rectangle,
    RoundedRectangle,
    PillCapsule,
    Ellipse,
    Parallelogram,
    Line
}

public enum TextBindingField
{
    Title,          // Speaker Name or Headline
    Subtitle,       // Speaker Title or Sermon Subtitle
    TagText,        // Badge (e.g., SPEAKER, READING)
    Reference,      // Scripture Reference (e.g., John 3:16)
    VerseText       // Scripture Verse Text
}
