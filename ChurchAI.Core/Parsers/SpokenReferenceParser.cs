using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChurchAI.Core.Interfaces;
using ChurchAI.Core.Models;

namespace ChurchAI.Core.Parsers;

public class SpokenReferenceParser : IBibleReferenceParser
{
    private string? _pendingBookName = null;
    private DateTime _pendingBookTimestamp = DateTime.MinValue;
    private readonly object _pendingLock = new object();
    private static readonly TimeSpan ContextTimeout = TimeSpan.FromSeconds(5);

    public void SetPendingBook(string bookName)
    {
        lock (_pendingLock)
        {
            _pendingBookName = bookName;
            _pendingBookTimestamp = DateTime.UtcNow;
        }
    }

    public string? GetPendingBook()
    {
        lock (_pendingLock)
        {
            if (_pendingBookName != null && (DateTime.UtcNow - _pendingBookTimestamp) <= ContextTimeout)
            {
                return _pendingBookName;
            }
            _pendingBookName = null;
            return null;
        }
    }

    public void ClearPendingBook()
    {
        lock (_pendingLock)
        {
            _pendingBookName = null;
        }
    }

    public BibleReference? ParseWithContext(string spokenText)
    {
        if (string.IsNullOrWhiteSpace(spokenText)) return null;

        // 1. Try standard full parse
        var directResult = Parse(spokenText);
        if (directResult != null)
        {
            if (directResult.Chapter > 0 || directResult.Verse.HasValue)
            {
                // Full reference with numbers parsed! Clear pending book.
                ClearPendingBook();
                return directResult;
            }
            else
            {
                // Result is just the book name (e.g. "John" or "Genesis")! Store in pending state.
                SetPendingBook(directResult.BookName);
                return directResult;
            }
        }

        // 2. Check if text is numbers-only or orphaned numbers while a pending book context is active
        string? activeBook = GetPendingBook();
        if (!string.IsNullOrEmpty(activeBook))
        {
            string combined = $"{activeBook} {spokenText}";
            var combinedResult = Parse(combined);
            if (combinedResult != null && (combinedResult.Chapter > 0 || combinedResult.Verse.HasValue))
            {
                ClearPendingBook();
                return combinedResult;
            }
        }

        return null;
    }

    private static readonly Dictionary<string, int> WordsToNumbers = new(StringComparer.OrdinalIgnoreCase)
    {
        {"zero", 0}, {"one", 1}, {"two", 2}, {"three", 3}, {"four", 4}, {"five", 5}, {"six", 6}, {"seven", 7}, {"eight", 8}, {"nine", 9},
        {"ten", 10}, {"eleven", 11}, {"twelve", 12}, {"thirteen", 13}, {"fourteen", 14}, {"fifteen", 15}, {"sixteen", 16}, {"seventeen", 17}, {"eighteen", 18}, {"nineteen", 19},
        {"twenty", 20}, {"thirty", 30}, {"forty", 40}, {"fifty", 50}, {"sixty", 60}, {"seventy", 70}, {"eighty", 80}, {"ninety", 90},
        {"hundred", 100},
        {"first", 1}, {"second", 2}, {"third", 3}, {"fourth", 4}, {"fifth", 5}
    };

    private static readonly List<string> NoiseWords = new() { "chapter", "verse", "book" };

    private static readonly List<string> BookNames = new()
    {
        "1 chronicles", "2 chronicles", "1 corinthians", "2 corinthians",
        "1 john", "2 john", "3 john", "1 kings", "2 kings",
        "1 peter", "2 peter", "1 samuel", "2 samuel", 
        "1 thessalonians", "2 thessalonians", "1 timothy", "2 timothy", 
        "song of solomon", "song of songs", "song solomon", "song songs", "canticles", "canticle of canticles", "canticle",
        "genesis", "exodus", "leviticus", "numbers", "deuteronomy",
        "joshua", "judges", "ruth", "ezra", "nehemiah", "esther", "job",
        "psalms", "psalm", "proverbs", "ecclesiastes", "isaiah", "jeremiah",
        "lamentations", "ezekiel", "daniel", "hosea", "joel", "amos",
        "obadiah", "jonah", "micah", "nahum", "habakkuk", "zephaniah",
        "haggai", "zechariah", "malachi", "matthew", "mark", "luke", "john",
        "acts", "romans", "galatians", "ephesians", "philippians", "colossians",
        "titus", "philemon", "hebrews", "james", "jude", "revelation"
    };

    public BibleReference? Parse(string spokenText)
    {
        if (string.IsNullOrWhiteSpace(spokenText)) return null;

        string normalized = spokenText.ToLowerInvariant().Trim();
        
        // Strip common LLM prefixes
        var prefixesToRemove = new[] { "output:", "verse:", "reference:", "the reference is:", "output", "the reference is" };
        foreach (var prefix in prefixesToRemove)
        {
            if (normalized.StartsWith(prefix))
            {
                normalized = normalized.Substring(prefix.Length).Trim();
                break;
            }
        }
        
        normalized = NormalizeNumbers(normalized);
        normalized = RemoveNoiseWords(normalized);

        // At this point "first corinthians thirteen four" is "1 corinthians 13 4"
        // Let's find the book
        string? matchedBook = null;
        int bookIndex = -1;
        
        // Order by length descending so "1 corinthians" matches before "corinthians"
        foreach (var book in BookNames.OrderByDescending(b => b.Length))
        {
            var pattern = @"\b" + Regex.Escape(book) + @"\b";
            var match = Regex.Match(normalized, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                matchedBook = book;
                bookIndex = match.Index;
                break;
            }
        }

        if (matchedBook == null) return null;

        // Remove the book from the string (and everything before it)
        string remainder = normalized.Substring(bookIndex + matchedBook.Length).Trim();
        
        // Capitalize book properly
        string formattedBook = FormatBookName(matchedBook);

        var reference = new BibleReference { BookName = formattedBook };

        if (string.IsNullOrWhiteSpace(remainder))
        {
            return reference; // Just the book
        }

        // Remainder should be numbers separated by spaces or colons. e.g. "13 4" or "13" or "13:4"
        var parts = remainder.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length >= 1 && int.TryParse(parts[0], out int chapter))
        {
            reference.Chapter = chapter;
        }

        if (parts.Length >= 2 && int.TryParse(parts[1], out int verse))
        {
            reference.Verse = verse;
        }

        return reference;
    }

    private string NormalizeNumbers(string input)
    {
        // Remove punctuation like commas and periods
        input = new string(input.Where(c => !char.IsPunctuation(c) || c == ':').ToArray());
        var tokens = input.ToLower().Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        
        int currentNumber = 0;
        int lastVal = 0;
        bool buildingNumber = false;
        
        foreach (var token in tokens)
        {
            if (int.TryParse(token, out int parsed))
            {
                if (buildingNumber)
                {
                    result.Add(currentNumber.ToString());
                    currentNumber = 0;
                    buildingNumber = false;
                    lastVal = 0;
                }
                result.Add(parsed.ToString());
                continue;
            }

            if (WordsToNumbers.TryGetValue(token, out int val))
            {
                bool canCombine = false;
                if (buildingNumber)
                {
                    if (val == 100) canCombine = true;
                    else if (lastVal == 100 && val < 100) canCombine = true;
                    else if (lastVal >= 20 && lastVal <= 90 && lastVal % 10 == 0 && val > 0 && val < 10) canCombine = true;
                }

                if (buildingNumber && !canCombine)
                {
                    result.Add(currentNumber.ToString());
                    currentNumber = 0;
                    lastVal = 0;
                }

                if (val == 100)
                {
                    if (currentNumber == 0) currentNumber = 100;
                    else currentNumber *= 100;
                }
                else
                {
                    currentNumber += val;
                }
                
                lastVal = val;
                buildingNumber = true;
            }
            else
            {
                if (buildingNumber)
                {
                    result.Add(currentNumber.ToString());
                    currentNumber = 0;
                    buildingNumber = false;
                    lastVal = 0;
                }
                result.Add(token);
            }
        }
        
        if (buildingNumber)
        {
            result.Add(currentNumber.ToString());
        }
        
        return string.Join(" ", result);
    }

    private string RemoveNoiseWords(string input)
    {
        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var filtered = tokens.Where(t => !NoiseWords.Contains(t, StringComparer.OrdinalIgnoreCase));
        return string.Join(" ", filtered);
    }

    private string FormatBookName(string book)
    {
        var lower = book.Trim().ToLowerInvariant();
        if (lower == "psalm" || lower == "psalms") return "Psalms";
        if (lower == "song of solomon" || lower == "song of songs" || lower == "song solomon" || lower == "song songs" || lower == "canticles" || lower == "canticle of canticles" || lower == "canticle") 
            return "Song of Solomon";

        var words = book.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0 && !int.TryParse(words[i], out _))
            {
                if (words[i].Equals("of", StringComparison.OrdinalIgnoreCase) || words[i].Equals("the", StringComparison.OrdinalIgnoreCase))
                {
                    words[i] = words[i].ToLower();
                }
                else
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
        }
        return string.Join(" ", words);
    }
}
