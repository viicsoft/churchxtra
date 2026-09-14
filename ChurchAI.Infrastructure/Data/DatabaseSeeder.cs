using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChurchAI.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly BibleDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;
    private const string KjvJsonUrl = "https://raw.githubusercontent.com/thiagobodruk/bible/master/json/en_kjv.json";
    private const string BbeJsonUrl = "https://raw.githubusercontent.com/thiagobodruk/bible/master/json/en_bbe.json";
    private const string RvrJsonUrl = "https://raw.githubusercontent.com/thiagobodruk/bible/master/json/es_rvr.json";
    private const string FrJsonUrl = "https://raw.githubusercontent.com/thiagobodruk/bible/master/json/fr_apee.json";

    public DatabaseSeeder(BibleDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedBibleAsync()
    {
        _logger.LogInformation("Starting database seeding process...");

        try
        {
            using var httpClient = new HttpClient();
            if (!await _context.Translations.AnyAsync(t => t.Abbreviation.ToUpper() == "KJV"))
            {
                await SeedTranslationAsync(httpClient, KjvJsonUrl, "King James Version", "KJV");
            }
            if (!await _context.Translations.AnyAsync(t => t.Abbreviation.ToUpper() == "BBE"))
            {
                await SeedTranslationAsync(httpClient, BbeJsonUrl, "Bible in Basic English", "BBE");
            }
            
            var localTranslations = new (string Path, string Name, string Abbr, bool IsScrollmapper)[]
            {
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_asv.json", "American Standard Version", "ASV", false),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_web.json", "World English Bible", "WEB", false),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_bsb.json", "Berean Standard Bible", "BSB", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_ylt.json", "Young's Literal Translation", "YLT", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_darby.json", "Darby Bible", "Darby", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_geneva1599.json", "Geneva Bible 1599", "Geneva1599", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_akjv.json", "American King James Version", "AKJV", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_cpdv.json", "Catholic Public Domain Version", "CPDV", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_drc.json", "Douay-Rheims Challoner Bible", "DRC", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_leb.json", "Lexham English Bible", "LEB", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_ukjv.json", "Updated King James Version", "UKJV", true),
                (@"C:\Churchxtra\ChurchAI.Infrastructure\Data\en_webster.json", "Webster Bible", "Webster", true)
            };

            foreach (var item in localTranslations)
            {
                if (File.Exists(item.Path) && !await _context.Translations.AnyAsync(t => t.Abbreviation.ToUpper() == item.Abbr.ToUpper()))
                {
                    string json = await File.ReadAllTextAsync(item.Path);
                    if (item.IsScrollmapper)
                    {
                        await SeedScrollmapperTranslationFromJsonAsync(json, item.Name, item.Abbr);
                    }
                    else
                    {
                        await SeedTranslationFromJsonAsync(json, item.Name, item.Abbr);
                    }
                }
            }

            _logger.LogInformation("Successfully seeded Bibles.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed database.");
        }
    }

    public async Task SeedHymnsAsync()
    {
        try
        {
            if (await _context.Hymns.AnyAsync())
            {
                return;
            }

            _logger.LogInformation("Seeding Hymns from local file...");
            string path = @"C:\Churchxtra\ChurchAI.Infrastructure\Data\hyms.txt";
            if (!File.Exists(path))
            {
                _logger.LogWarning("hyms.txt not found, skipping seeder.");
                return;
            }

            var lines = await File.ReadAllLinesAsync(path);
            var hymns = new List<Hymn>();
            Hymn? currentHymn = null;
            var lyricsBuilder = new System.Text.StringBuilder();

            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    if (currentHymn != null && lyricsBuilder.Length > 0)
                    {
                        lyricsBuilder.AppendLine();
                    }
                    continue;
                }

                // Check if this line is a hymn number (e.g. 001, 002, or just a number)
                if (int.TryParse(trimmed, out int num) && num > 0 && num < 1000)
                {
                    // Save previous hymn if any
                    if (currentHymn != null)
                    {
                        currentHymn.Lyrics = lyricsBuilder.ToString().Trim();
                        if (string.IsNullOrEmpty(currentHymn.Title))
                        {
                            currentHymn.Title = ExtractTitleFromLyrics(currentHymn.Lyrics);
                        }
                        hymns.Add(currentHymn);
                    }

                    currentHymn = new Hymn
                    {
                        Number = num,
                        Title = string.Empty
                    };
                    lyricsBuilder.Clear();
                }
                else
                {
                    if (currentHymn != null)
                    {
                        lyricsBuilder.AppendLine(rawLine);
                    }
                }
            }

            // Save the last hymn
            if (currentHymn != null)
            {
                currentHymn.Lyrics = lyricsBuilder.ToString().Trim();
                if (string.IsNullOrEmpty(currentHymn.Title))
                {
                    currentHymn.Title = ExtractTitleFromLyrics(currentHymn.Lyrics);
                }
                hymns.Add(currentHymn);
            }

            if (hymns.Any())
            {
                await _context.Hymns.AddRangeAsync(hymns);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully seeded {hymns.Count} hymns from file.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed hymns from file.");
        }
    }

    private string ExtractTitleFromLyrics(string lyrics)
    {
        if (string.IsNullOrWhiteSpace(lyrics)) return "Untitled Hymn";

        var firstLine = lyrics.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();

        if (string.IsNullOrEmpty(firstLine)) return "Untitled Hymn";

        var cleaned = firstLine;
        if (char.IsDigit(cleaned[0]))
        {
            int index = 0;
            while (index < cleaned.Length && (char.IsDigit(cleaned[index]) || cleaned[index] == '.' || char.IsWhiteSpace(cleaned[index])))
            {
                index++;
            }
            cleaned = cleaned.Substring(index).Trim();
        }

        cleaned = cleaned.TrimEnd(',', '.', ';', ':', '!', '?');

        return string.IsNullOrEmpty(cleaned) ? "Untitled Hymn" : cleaned;
    }

    private async Task SeedTranslationAsync(HttpClient httpClient, string url, string name, string abbreviation)
    {
        _logger.LogInformation($"Seeding {abbreviation} from {url}...");
        try 
        {
            var jsonString = await httpClient.GetStringAsync(url);
            var bibleBooks = JsonSerializer.Deserialize<List<BibleBookJson>>(jsonString);

            if (bibleBooks == null) return;

            var translation = new Translation
            {
                Name = name,
                Abbreviation = abbreviation,
                Language = "English"
            };

            _context.Translations.Add(translation);
            await _context.SaveChangesAsync();

            var booksToAdd = new List<Book>();

            foreach (var bookJson in bibleBooks)
            {
                var book = new Book
                {
                    Name = bookJson.Name ?? "",
                    TranslationId = translation.Id,
                    Verses = new List<Verse>()
                };

                if (bookJson.Chapters != null)
                {
                    for (int chIndex = 0; chIndex < bookJson.Chapters.Count; chIndex++)
                    {
                        var chapterVerses = bookJson.Chapters[chIndex];
                        int chapterNumber = chIndex + 1;

                        for (int vIndex = 0; vIndex < chapterVerses.Count; vIndex++)
                        {
                            book.Verses.Add(new Verse
                            {
                                Chapter = chapterNumber,
                                VerseNumber = vIndex + 1,
                                Text = chapterVerses[vIndex]
                            });
                        }
                    }
                }
                booksToAdd.Add(book);
            }

            _context.Books.AddRange(booksToAdd);
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to seed translation {abbreviation} from {url}");
        }
    }
    private async Task SeedTranslationFromJsonAsync(string jsonString, string name, string abbreviation)
    {
        _logger.LogInformation($"Seeding {abbreviation} from local JSON...");
        try 
        {
            var bibleBooks = JsonSerializer.Deserialize<List<BibleBookJson>>(jsonString);

            if (bibleBooks == null) return;

            var translation = new Translation
            {
                Name = name,
                Abbreviation = abbreviation,
                Language = "English"
            };

            _context.Translations.Add(translation);
            await _context.SaveChangesAsync();

            var booksToAdd = new List<Book>();

            foreach (var bookJson in bibleBooks)
            {
                var book = new Book
                {
                    Name = bookJson.Name ?? "",
                    TranslationId = translation.Id,
                    Verses = new List<Verse>()
                };

                if (bookJson.Chapters != null)
                {
                    for (int chIndex = 0; chIndex < bookJson.Chapters.Count; chIndex++)
                    {
                        var chapterVerses = bookJson.Chapters[chIndex];
                        int chapterNumber = chIndex + 1;

                        for (int vIndex = 0; vIndex < chapterVerses.Count; vIndex++)
                        {
                            book.Verses.Add(new Verse
                            {
                                Chapter = chapterNumber,
                                VerseNumber = vIndex + 1,
                                Text = chapterVerses[vIndex]
                            });
                        }
                    }
                }
                booksToAdd.Add(book);
            }

            _context.Books.AddRange(booksToAdd);
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to seed translation {abbreviation} from local JSON");
        }
    }

    public async Task SeedSpokenPhrasesAsync()
    {
        if (await _context.SpokenPhrases.AnyAsync())
        {
            return;
        }

        _logger.LogInformation("Generating 100,000+ N-Gram phrase index from Bible verses...");

        try
        {
            int kjvTranslationId = await _context.Translations
                .Where(t => t.Abbreviation.ToUpper() == "KJV")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            if (kjvTranslationId == 0)
            {
                kjvTranslationId = 1;
            }

            var verses = await _context.Verses
                .Include(v => v.Book)
                .Where(v => v.Book!.TranslationId == kjvTranslationId)
                .Select(v => new { v.Book!.Name, v.Chapter, v.VerseNumber, v.Text })
                .ToListAsync();

            var phraseSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var phraseEntities = new List<SpokenPhrase>(120000);

            foreach (var verse in verses)
            {
                if (string.IsNullOrWhiteSpace(verse.Text)) continue;
                string refKey = $"{verse.Name} {verse.Chapter}:{verse.VerseNumber}";

                char[] cleanedChars = verse.Text.ToLowerInvariant()
                    .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                    .ToArray();
                string cleanedText = new string(cleanedChars);

                string[] words = cleanedText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 3) continue;

                for (int n = 3; n <= 6; n++)
                {
                    for (int i = 0; i <= words.Length - n; i++)
                    {
                        string phrase = string.Join(" ", words, i, n);
                        if (phrase.Length < 10) continue;

                        if (phraseSet.Add(phrase))
                        {
                            phraseEntities.Add(new SpokenPhrase
                            {
                                NormalizedPhrase = phrase,
                                ReferenceKey = refKey
                            });

                            if (phraseEntities.Count >= 120000) break;
                        }
                    }
                    if (phraseEntities.Count >= 120000) break;
                }
            }

            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            int batchSize = 10000;
            for (int i = 0; i < phraseEntities.Count; i += batchSize)
            {
                var batch = phraseEntities.GetRange(i, Math.Min(batchSize, phraseEntities.Count - i));
                await _context.SpokenPhrases.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
            }
            _context.ChangeTracker.AutoDetectChangesEnabled = true;

            _logger.LogInformation($"Successfully seeded {phraseEntities.Count} unique spoken phrases into database!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed spoken phrases.");
        }
    }

    private async Task SeedScrollmapperTranslationFromJsonAsync(string jsonContent, string translationName, string abbreviation)
    {
        try
        {
            if (await _context.Translations.AnyAsync(t => t.Abbreviation.ToUpper() == abbreviation.ToUpper()))
            {
                _logger.LogInformation($"Translation {abbreviation} already seeded.");
                return;
            }

            _logger.LogInformation($"Seeding translation {translationName} ({abbreviation}) from local Scrollmapper JSON...");
            var bibleJson = JsonSerializer.Deserialize<ScrollmapperBibleJson>(jsonContent);
            if (bibleJson?.Books == null)
            {
                _logger.LogWarning($"Scrollmapper JSON for {abbreviation} was empty or invalid.");
                return;
            }

            var translation = new Translation
            {
                Name = translationName,
                Abbreviation = abbreviation
            };
            _context.Translations.Add(translation);
            await _context.SaveChangesAsync();

            var booksToAdd = new List<Book>();

            foreach (var bookJson in bibleJson.Books)
            {
                if (string.IsNullOrWhiteSpace(bookJson.Name)) continue;

                var book = new Book
                {
                    Name = bookJson.Name,
                    TranslationId = translation.Id,
                    Verses = new List<Verse>()
                };

                if (bookJson.Chapters != null)
                {
                    foreach (var chapterJson in bookJson.Chapters)
                    {
                        if (chapterJson.Verses == null) continue;

                        foreach (var verseJson in chapterJson.Verses)
                        {
                            if (string.IsNullOrWhiteSpace(verseJson.Text)) continue;

                            book.Verses.Add(new Verse
                            {
                                Chapter = chapterJson.Chapter > 0 ? chapterJson.Chapter : 1,
                                VerseNumber = verseJson.Verse > 0 ? verseJson.Verse : 1,
                                Text = verseJson.Text
                            });
                        }
                    }
                }
                booksToAdd.Add(book);
            }

            _context.Books.AddRange(booksToAdd);
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
            _logger.LogInformation($"Successfully seeded translation {abbreviation} with {booksToAdd.Count} books.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to seed translation {abbreviation} from Scrollmapper JSON.");
        }
    }
}

public class BibleBookJson
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("abbrev")]
    public string? Abbrev { get; set; }

    [JsonPropertyName("chapters")]
    public List<List<string>>? Chapters { get; set; }
}

public class ScrollmapperBibleJson
{
    [JsonPropertyName("books")]
    public List<ScrollmapperBookJson>? Books { get; set; }
}

public class ScrollmapperBookJson
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("chapters")]
    public List<ScrollmapperChapterJson>? Chapters { get; set; }
}

public class ScrollmapperChapterJson
{
    [JsonPropertyName("chapter")]
    public int Chapter { get; set; }

    [JsonPropertyName("verses")]
    public List<ScrollmapperVerseJson>? Verses { get; set; }
}

public class ScrollmapperVerseJson
{
    [JsonPropertyName("verse")]
    public int Verse { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
