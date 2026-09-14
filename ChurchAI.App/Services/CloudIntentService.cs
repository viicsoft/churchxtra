using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Interfaces;

namespace ChurchAI.App.Services;

public class CloudIntentService : IAIIntentService
{
    private readonly HttpClient _httpClient;
    private readonly IBibleReferenceParser _parser;
    private readonly ISettingsService _settings;

    private static readonly string[] BibleBookKeywords = new[]
    {
        "genesis", "exodus", "leviticus", "numbers", "deuteronomy", "joshua", "judges", "ruth", "samuel", "kings", "chronicles", 
        "ezra", "nehemiah", "esther", "job", "psalm", "proverbs", "ecclesiastes", "song", "isaiah", "jeremiah", "lamentations", 
        "ezekiel", "daniel", "hosea", "joel", "obadiah", "jonah", "micah", "nahum", "habakkuk", "zephaniah", "haggai", "zechariah", 
        "malachi", "matthew", "mark", "luke", "john", "acts", "romans", "corinthians", "galatians", "ephesians", "philippians", 
        "colossians", "thessalonians", "timothy", "titus", "philemon", "hebrews", "james", "peter", "jude", "revelation",
        
        "gen", "exod", "lev", "num", "deut", "josh", "judg", "sam", "kings", "chron", "neh", "est", "psa", "prov", "ecc", 
        "isa", "jer", "lam", "ezek", "dan", "hos", "obad", "jon", "mic", "nah", "hab", "zeph", "hag", "zech", "mal", 
        "matt", "rom", "cor", "gal", "eph", "phil", "col", "thess", "tim", "tit", "heb", "jas", "pet", "rev",

        // Common story terms to pass the pre-filter for topical search
        "jesus", "david", "goliath", "cain", "abel", "moses", "noah", "creation", "commandments", 
        "god", "lord", "adam", "eve", "abraham", "isaac", "jacob", "joseph", "solomon", "paul", 
        "christ", "mary", "ark", "flood", "wept", "baptist", "cross", "annoints", "anoint", "anointed", "kills", "killed"
    };

    public CloudIntentService(HttpClient httpClient, IBibleReferenceParser parser, ISettingsService settings)
    {
        _httpClient = httpClient;
        _parser = parser;
        _settings = settings;
    }

    public async Task<string?> ExtractBibleReferenceIntentAsync(string spokenText)
    {
        if (string.IsNullOrWhiteSpace(spokenText) || string.IsNullOrWhiteSpace(_settings.CloudApiKey)) return null;

        var normalizedInput = new string(spokenText.ToLowerInvariant().Where(c => !char.IsPunctuation(c)).ToArray()).Trim();

        // Fast-path for common topical queries to provide instant responses without AI overhead
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
        
        // Local Pre-Filter: Ensure the spoken text contains at least one Bible book or topical keyword.
        // This stops background noise / conversational filler from calling the LLM and hallucinating.
        bool hasBookKeyword = false;
        foreach (var keyword in BibleBookKeywords)
        {
            if (normalizedInput.Contains(keyword))
            {
                hasBookKeyword = true;
                break;
            }
        }

        if (!hasBookKeyword)
        {
            return null; // Ignore general chatter
        }

        var systemPrompt = @"You are a strict Bible reference extraction AI. Your goal is to identify and extract all explicit Bible references (Book, Chapter, and optionally Verse) or map famous Bible stories, events, characters, or phrases to their primary corresponding scripture verses from natural speech.

Rules:
1. Extract explicit references if the speech mentions a Bible book name accompanied by numbers (e.g. 'John chapter 3 verse 16' -> 'john 3 16', 'Genesis 1:1' -> 'genesis 1 1').
2. If the user mentions famous biblical stories, topics, phrases, events, or names (e.g. 'David and Goliath', 'creation', 'Cain and Abel', 'Jesus feeds the five thousand'), map them to their primary corresponding Bible references.
3. You can extract and return MULTIPLE references if more than one is mentioned in the speech. Separate multiple references with a comma (e.g., 'john 3 16, genesis 1 1, 1 samuel 17 50').
4. If the speech is completely unrelated to Bible books or famous Bible stories, output exactly NONE.
5. Do not provide explanations, conversational text, or punctuation. Output ONLY the reference(s) or NONE.

Examples:
Input: let's open to the book of John chapter 3 verse 16 and also remember Genesis 1:1
Output: john 3 16, genesis 1 1
Input: david kills goliath and cain killed abel
Output: 1 samuel 17 50, genesis 4 8
Input: testing the microphone
Output: NONE
Input: we read in Psalms 23 and John 11:35
Output: psalms 23, john 11 35";

        var payload = new
        {
            model = _settings.IntentApiModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = spokenText }
            },
            temperature = 0.0
        };

        string apiKey = !string.IsNullOrWhiteSpace(_settings.IntentApiKey) ? _settings.IntentApiKey : _settings.CloudApiKey;
        var request = new HttpRequestMessage(HttpMethod.Post, _settings.IntentApiUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(payload);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseBody);
            
            var messageContent = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(messageContent) || messageContent.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            messageContent = messageContent.Replace("'", "").Replace("\"", "").Replace(".", "");
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

            return $"PARSE_ERROR: {messageContent}";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }
}
