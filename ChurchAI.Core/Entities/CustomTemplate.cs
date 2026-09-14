using System;
using System.Collections.Generic;

namespace ChurchAI.Core.Entities;

public class CustomTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "My Custom Template";
    public CustomTemplateCategory Category { get; set; } = CustomTemplateCategory.Speaker;
    public double CanvasWidth { get; set; } = 1920;
    public double CanvasHeight { get; set; } = 1080;
    public List<CustomVectorShape> Shapes { get; set; } = new();
    public List<CustomTextPlaceholder> TextPlaceholders { get; set; } = new();
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateModified { get; set; } = DateTime.UtcNow;
}
