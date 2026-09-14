using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChurchAI.App.ViewModels;

public partial class CustomTemplateBuilderViewModel : ViewModelBase
{
    private readonly ICustomTemplateService _templateService;
    private readonly IAITemplateExtractorService _aiExtractorService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _templateId = string.Empty;

    [ObservableProperty]
    private string _templateName = "My Custom Lower Third";

    [ObservableProperty]
    private CustomTemplateCategory _category = CustomTemplateCategory.Speaker;

    [ObservableProperty]
    private string _statusMessage = "Ready to edit template.";

    public ObservableCollection<CustomVectorShape> Shapes { get; } = new();
    public ObservableCollection<CustomTextPlaceholder> TextPlaceholders { get; } = new();

    [ObservableProperty]
    private CustomVectorShape? _selectedShape;

    [ObservableProperty]
    private CustomTextPlaceholder? _selectedTextPlaceholder;

    public Action? RequestClose { get; set; }

    public CustomTemplateBuilderViewModel(
        ICustomTemplateService templateService,
        IAITemplateExtractorService aiExtractorService,
        ISettingsService settingsService)
    {
        _templateService = templateService;
        _aiExtractorService = aiExtractorService;
        _settingsService = settingsService;
    }

    public void LoadTemplate(CustomTemplate template)
    {
        TemplateId = template.Id;
        TemplateName = template.Name;
        Category = template.Category;

        Shapes.Clear();
        foreach (var s in template.Shapes) Shapes.Add(s);

        TextPlaceholders.Clear();
        foreach (var t in template.TextPlaceholders) TextPlaceholders.Add(t);

        SelectedShape = Shapes.FirstOrDefault();
        SelectedTextPlaceholder = TextPlaceholders.FirstOrDefault();
        StatusMessage = $"Loaded template '{TemplateName}'";
    }

    [RelayCommand]
    private void AddRectangle()
    {
        var shape = new CustomVectorShape
        {
            Name = "New Rectangle",
            ShapeType = ShapeType.RoundedRectangle,
            X = 200, Y = 850, Width = 500, Height = 100,
            CornerRadius = 12, FillHex = "#0F172A", StrokeHex = "#38BDF8", StrokeThickness = 2
        };
        Shapes.Add(shape);
        SelectedShape = shape;
        SelectedTextPlaceholder = null;
        StatusMessage = "Added Rectangle shape layer.";
    }

    [RelayCommand]
    private void AddPillCapsule()
    {
        var shape = new CustomVectorShape
        {
            Name = "New Pill Badge",
            ShapeType = ShapeType.PillCapsule,
            X = 200, Y = 800, Width = 180, Height = 40,
            CornerRadius = 20, FillHex = "#3B82F6", StrokeThickness = 0
        };
        Shapes.Add(shape);
        SelectedShape = shape;
        SelectedTextPlaceholder = null;
        StatusMessage = "Added Pill Capsule shape layer.";
    }

    [RelayCommand]
    private void AddParallelogram()
    {
        var shape = new CustomVectorShape
        {
            Name = "Slanted Banner",
            ShapeType = ShapeType.Parallelogram,
            X = 200, Y = 850, Width = 600, Height = 90,
            SkewX = -25, FillHex = "#0038A8", StrokeHex = "#FFFFFF", StrokeThickness = 1
        };
        Shapes.Add(shape);
        SelectedShape = shape;
        SelectedTextPlaceholder = null;
        StatusMessage = "Added Slanted Parallelogram layer.";
    }

    [RelayCommand]
    private void AddEllipse()
    {
        var shape = new CustomVectorShape
        {
            Name = "Circle Accent",
            ShapeType = ShapeType.Ellipse,
            X = 140, Y = 860, Width = 80, Height = 80,
            FillHex = "#F59E0B", StrokeThickness = 0
        };
        Shapes.Add(shape);
        SelectedShape = shape;
        SelectedTextPlaceholder = null;
        StatusMessage = "Added Circle accent layer.";
    }

    [RelayCommand]
    private void AddTextPlaceholder()
    {
        var text = new CustomTextPlaceholder
        {
            Name = "Title Element",
            BindingField = Category == CustomTemplateCategory.Speaker ? TextBindingField.Title : TextBindingField.VerseText,
            X = 220, Y = 870, Width = 460, Height = 50,
            FontSize = 28, FontWeight = "Bold", ForegroundHex = "#FFFFFF"
        };
        TextPlaceholders.Add(text);
        SelectedTextPlaceholder = text;
        SelectedShape = null;
        StatusMessage = "Added Text placeholder layer.";
    }

    [RelayCommand]
    private void RemoveSelectedLayer()
    {
        if (SelectedShape != null)
        {
            Shapes.Remove(SelectedShape);
            SelectedShape = Shapes.LastOrDefault();
            StatusMessage = "Removed shape layer.";
        }
        else if (SelectedTextPlaceholder != null)
        {
            TextPlaceholders.Remove(SelectedTextPlaceholder);
            SelectedTextPlaceholder = TextPlaceholders.LastOrDefault();
            StatusMessage = "Removed text layer.";
        }
    }

    [RelayCommand]
    private async Task ImportFromImageAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|All Files (*.*)|*.*",
            Title = "Select Broadcast Lower Third Screenshot for AI Extraction"
        };

        if (dlg.ShowDialog() == true)
        {
            StatusMessage = "⏳ Analyzing image layout with AI Vision...";
            try
            {
                var extracted = await _aiExtractorService.ExtractTemplateFromImageAsync(dlg.FileName, Category);
                LoadTemplate(extracted);
                if (string.IsNullOrWhiteSpace(_settingsService.GeminiVisionApiKey))
                {
                    StatusMessage = "⚠️ Loaded AI Draft Template! (Tip: Add your Gemini API Key in Settings for full AI vision extraction).";
                }
                else
                {
                    StatusMessage = "✅ Successfully extracted template design from image with AI!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Image extraction error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    public async Task SaveTemplateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TemplateName))
            {
                TemplateName = "Custom Template";
            }

            var template = new CustomTemplate
            {
                Id = string.IsNullOrWhiteSpace(TemplateId) ? Guid.NewGuid().ToString() : TemplateId,
                Name = TemplateName,
                Category = Category,
                CanvasWidth = 1920,
                CanvasHeight = 1080,
                Shapes = Shapes.ToList(),
                TextPlaceholders = TextPlaceholders.ToList(),
                DateModified = DateTime.UtcNow
            };

            await _templateService.SaveTemplateAsync(template);
            StatusMessage = $"Saved template '{TemplateName}' successfully!";

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                RequestClose?.Invoke();
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error saving template: {ex.Message}";
        }
    }
}
