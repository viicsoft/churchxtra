using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ChurchAI.Core.Interfaces;
using ChurchAI.Core.Models;

namespace ChurchAI.AI.Services;

public class OllamaIntentService : IAIIntentService
{
    private readonly HttpClient _httpClient;
    private readonly IBibleReferenceParser _parser;
    private string _resolvedModelName = "gemma";
    private bool _modelResolved = false;

    public OllamaIntentService(HttpClient httpClient, IBibleReferenceParser parser)
    {
        _httpClient = httpClient;
        _parser = parser;
    }

    private async Task EnsureModelResolvedAsync()
    {
        if (_modelResolved) return;
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("models", out var modelsProperty) && modelsProperty.ValueKind == JsonValueKind.Array)
                {
                    var names = modelsProperty.EnumerateArray()
                        .Select(m => m.TryGetProperty("name", out var n) ? n.GetString() : null)
                        .Where(n => n != null)
                        .ToList();

                    // Prioritize fast, lightweight models suitable for 8GB RAM systems first
                    if (names.Contains("qwen2.5:0.5b"))
                    {
                        _resolvedModelName = "qwen2.5:0.5b";
                    }
                    else if (names.Contains("llama3.2:1b"))
                    {
                        _resolvedModelName = "llama3.2:1b";
                    }
                    else if (names.Contains("gemma:2b"))
                    {
                        _resolvedModelName = "gemma:2b";
                    }
                    else if (names.Contains("gemma:latest"))
                    {
                        _resolvedModelName = "gemma:latest";
                    }
                    else if (names.Contains("gemma"))
                    {
                        _resolvedModelName = "gemma";
                    }
                    else
                    {
                        var firstModel = names.FirstOrDefault();
                        if (firstModel != null)
                        {
                            _resolvedModelName = firstModel;
                        }
                    }
                }
            }
        }
        catch {}
        _modelResolved = true;
    }

    public async Task<string?> ExtractBibleReferenceIntentAsync(string spokenText)
    {
        if (string.IsNullOrWhiteSpace(spokenText)) return null;
        
        await EnsureModelResolvedAsync();
        
        // Fast-path for common topical queries to provide instant responses without AI overhead
        var normalizedInput = new string(spokenText.ToLowerInvariant().Where(c => !char.IsPunctuation(c)).ToArray()).Trim();
        var commonTopics = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "jesus feeds 5000", "matthew 14 15" },
            { "jesus feeds five thousand", "matthew 14 15" },
            { "jesus wept", "john 11 35" },
            { "creation of the world", "genesis 1 1" },
            { "the lords prayer", "matthew 6 9" },
            { "lords prayer", "matthew 6 9" },
            { "the ten commandments", "exodus 20 2" },
            { "ten commandments", "exodus 20 2" },
            { "the beatitudes", "matthew 5 3" },
            { "beatitudes", "matthew 5 3" },
            { "birth of jesus", "luke 2 1" },
            { "resurrection of jesus", "luke 24 1" },
            { "jesus walks on water", "matthew 14 25" },
            { "david and goliath", "1 samuel 17 4" },
            { "samuel anoints david", "1 samuel 16 13" },
            { "samuel annoints david", "1 samuel 16 13" },
            { "david kills goliath", "1 samuel 17 50" },
            { "cain killed abel", "genesis 4 8" },
            { "cain kills abel", "genesis 4 8" }
        };

        if (commonTopics.TryGetValue(normalizedInput, out var knownReference))
        {
            var parsed = _parser.Parse(knownReference);
            if (parsed != null)
            {
                return parsed.ToString();
            }
        }

        var systemPrompt = @"You are an advanced Bible reference extraction AI. Your goal is to identify Bible verses or biblical concepts mentioned within natural speech and sentences, and extract ONLY the corresponding Bible references.

Rules:
1. If the user explicitly mentions a verse (e.g., 'let's open to john 3:16'), extract the reference.
2. If the user mentions a biblical event or phrase (e.g., 'jesus walked on water'), output the most famous verse reference for it.
3. You can extract and return MULTIPLE references if more than one is mentioned in the speech. Separate multiple references with a comma (e.g., 'john 3 16, genesis 1 1, 1 samuel 17 50').
4. Ignore all surrounding conversational filler words (e.g., 'brethrens', 'let's open to', 'because even').
5. If the text does not contain any Bible references or clear biblical concepts, output exactly NONE.
6. Do not provide explanations, conversational text, or punctuation. Output ONLY the reference(s) or NONE.
7. The input text is transcribed from speech and may contain phonetic errors. Try to correct these errors and extract the intended Bible reference.

Examples:
Input: lets open to the book of john 3:16 and remember genesis 1:1
Output: john 3 16, genesis 1 1
Input: look down
Output: luke 10
Input: brethrens lets have faith because even jesus walked on water and remember how cain killed abel
Output: matthew 14 25, genesis 4 8
Input: testing the microphone
Output: NONE";

        var payload = new
        {
            model = _resolvedModelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = spokenText }
            },
            stream = false,
            options = new
            {
                temperature = 0.0,
                num_predict = 40
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chat", payload);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseBody);
            
            var messageContent = document.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(messageContent) || 
                messageContent.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            
            // Clean up any weird punctuation AI might have added
            messageContent = messageContent.Replace("'", "").Replace("\"", "").Replace(".", "");

            // Validate that the extracted text is parseable by our strict parser
            var parts = messageContent.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var parsedList = new System.Collections.Generic.List<string>();
            foreach (var part in parts)
            {
                var parsedReference = _parser.Parse(part.Trim());
                if (parsedReference != null)
                {
                    parsedList.Add(parsedReference.ToString());
                }
            }

            if (parsedList.Any())
            {
                return string.Join(",", parsedList);
            }

            return null;
        }
        catch (Exception ex)
        {
            // If Ollama is offline or fails, return a descriptive error so the UI can notify the user
            System.Diagnostics.Debug.WriteLine($"Ollama error: {ex.Message}");
            return $"ERROR: Local Ollama offline ({ex.Message})";
        }
    }
}
