using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;

namespace ChurchAI.App.Services;

public class AITemplateExtractorService : IAITemplateExtractorService
{
    private readonly ISettingsService _settings;
    private readonly HttpClient _httpClient;

    public AITemplateExtractorService(ISettingsService settings)
    {
        _settings = settings;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<CustomTemplate> ExtractTemplateFromImageAsync(string imageFilePath, CustomTemplateCategory preferredCategory)
    {
        if (!File.Exists(imageFilePath))
        {
            throw new FileNotFoundException($"Image file not found: {imageFilePath}");
        }

        string apiKey = _settings.GeminiVisionApiKey;

        // Fallback draft template if no API key is provided
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return CreateDefaultDraftTemplate(preferredCategory, "Draft Cloned Template (Add Gemini API Key in Settings for AI Extraction)");
        }

        try
        {
            byte[] imageBytes = await File.ReadAllBytesAsync(imageFilePath);
            string base64Data = Convert.ToBase64String(imageBytes);
            string mimeType = GetMimeType(imageFilePath);

            string promptText = @"You are a professional broadcast graphic designer. Analyze this lower third graphic screenshot. 
Extract its design layout into JSON matching this exact structure:
{
  ""name"": ""Extracted Broadcast Template"",
  ""category"": """ + preferredCategory.ToString() + @""",
  ""shapes"": [
    {
      ""shapeType"": ""RoundedRectangle"",
      ""x"": 100, ""y"": 800, ""width"": 600, ""height"": 120, ""cornerRadius"": 16,
      ""fillHex"": ""#1E293B"", ""strokeHex"": ""#3B82F6"", ""strokeThickness"": 2, ""opacity"": 0.95, ""skewX"": -15
    }
  ],
  ""textPlaceholders"": [
    {
      ""bindingField"": ""Title"",
      ""x"": 130, ""y"": 820, ""width"": 540, ""height"": 40,
      ""fontFamily"": ""Segoe UI"", ""fontSize"": 28, ""fontWeight"": ""Bold"", ""foregroundHex"": ""#FFFFFF"", ""textAlignment"": ""Left""
    }
  ]
}
Return ONLY valid raw JSON with no markdown wrapping.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = promptText },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Data
                                }
                            }
                        }
                    }
                }
            };

            string requestJson = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string resJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(resJson);
                var textPart = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (!string.IsNullOrWhiteSpace(textPart))
                {
                    string cleanedJson = CleanJsonResponse(textPart);
                    var extracted = JsonSerializer.Deserialize<CustomTemplate>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (extracted != null)
                    {
                        extracted.Id = Guid.NewGuid().ToString();
                        extracted.Category = preferredCategory;
                        if (string.IsNullOrWhiteSpace(extracted.Name)) extracted.Name = "AI Extracted Template";
                        return extracted;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AI Extraction Exception: {ex.Message}");
        }

        return CreateDefaultDraftTemplate(preferredCategory, "AI Draft Cloned Template");
    }

    private static string CleanJsonResponse(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(7);
        }
        else if (text.StartsWith("```"))
        {
            text = text.Substring(3);
        }
        if (text.EndsWith("```"))
        {
            text = text.Substring(0, text.Length - 3);
        }
        return text.Trim();
    }

    private static string GetMimeType(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "image/png"
        };
    }

    private static CustomTemplate CreateDefaultDraftTemplate(CustomTemplateCategory category, string name)
    {
        var template = new CustomTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Category = category
        };

        if (category == CustomTemplateCategory.Speaker)
        {
            template.Shapes.Add(new CustomVectorShape
            {
                Name = "Main Tag Pill",
                ShapeType = ShapeType.PillCapsule,
                X = 100, Y = 840, Width = 160, Height = 40,
                FillHex = "#3B82F6", Opacity = 1.0, ZIndex = 1
            });

            template.Shapes.Add(new CustomVectorShape
            {
                Name = "Main Speaker Box",
                ShapeType = ShapeType.RoundedRectangle,
                X = 100, Y = 885, Width = 650, Height = 100,
                CornerRadius = 16, FillHex = "#0F172A", StrokeHex = "#38BDF8", StrokeThickness = 2, Opacity = 0.95, ZIndex = 2
            });

            template.TextPlaceholders.Add(new CustomTextPlaceholder
            {
                Name = "Speaker Tag",
                BindingField = TextBindingField.TagText,
                X = 120, Y = 847, Width = 120, Height = 25,
                FontSize = 14, FontWeight = "Bold", ForegroundHex = "#FFFFFF", TextAlignment = "Center", ZIndex = 10
            });

            template.TextPlaceholders.Add(new CustomTextPlaceholder
            {
                Name = "Speaker Title (Name)",
                BindingField = TextBindingField.Title,
                X = 130, Y = 895, Width = 600, Height = 45,
                FontSize = 30, FontWeight = "Bold", ForegroundHex = "#F8FAFC", TextAlignment = "Left", ZIndex = 11
            });

            template.TextPlaceholders.Add(new CustomTextPlaceholder
            {
                Name = "Speaker Subtitle",
                BindingField = TextBindingField.Subtitle,
                X = 130, Y = 942, Width = 600, Height = 35,
                FontSize = 20, FontWeight = "SemiBold", ForegroundHex = "#94A3B8", TextAlignment = "Left", ZIndex = 12
            });
        }
        else
        {
            template.Shapes.Add(new CustomVectorShape
            {
                Name = "Scripture Reference Badge",
                ShapeType = ShapeType.PillCapsule,
                X = 100, Y = 820, Width = 280, Height = 45,
                FillHex = "#F59E0B", Opacity = 1.0, ZIndex = 1
            });

            template.Shapes.Add(new CustomVectorShape
            {
                Name = "Scripture Verse Card",
                ShapeType = ShapeType.RoundedRectangle,
                X = 100, Y = 870, Width = 1100, Height = 140,
                CornerRadius = 20, FillHex = "#E5E3D8", StrokeHex = "#F59E0B", StrokeThickness = 2, Opacity = 0.98, ZIndex = 2
            });

            template.TextPlaceholders.Add(new CustomTextPlaceholder
            {
                Name = "Reference Text",
                BindingField = TextBindingField.Reference,
                X = 120, Y = 828, Width = 240, Height = 30,
                FontSize = 18, FontWeight = "Bold", ForegroundHex = "#0F172A", TextAlignment = "Center", ZIndex = 10
            });

            template.TextPlaceholders.Add(new CustomTextPlaceholder
            {
                Name = "Verse Text",
                BindingField = TextBindingField.VerseText,
                X = 130, Y = 885, Width = 1040, Height = 110,
                FontSize = 26, FontWeight = "SemiBold", ForegroundHex = "#1E293B", TextAlignment = "Left", ZIndex = 11
            });
        }

        return template;
    }
}
