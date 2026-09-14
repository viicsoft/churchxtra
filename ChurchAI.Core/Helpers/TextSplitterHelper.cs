using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChurchAI.Core.Helpers;

public static class TextSplitterHelper
{
    private static readonly Regex MarginalNoteRegex = new(@"\{[^}]*:[^}]*\}", RegexOptions.Compiled);
    private static readonly Regex SimpleBraceRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Cleans KJV/translation text by resolving inline braces:
    /// Marginal notes with colons e.g. {as one...: or, as one that is veiled} are removed.
    /// Simple word additions e.g. {thy flock} have their braces stripped.
    /// </summary>
    public static string CleanScriptureText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. Remove marginal notes containing colons e.g. {as one...: or, ...}
        string cleaned = MarginalNoteRegex.Replace(text, string.Empty);

        // 2. Unwrap simple word additions e.g. {thy flock} -> thy flock
        cleaned = SimpleBraceRegex.Replace(cleaned, "$1");

        // 3. Clean up horizontal spaces (preserving newlines for hymns/verses)
        cleaned = Regex.Replace(cleaned, @"[ \t]+", " ").Trim();
        cleaned = Regex.Replace(cleaned, @"[ \t]+([.,;:?!])", "$1");

        return cleaned;
    }

    /// <summary>
    /// Splits long text into natural chunks if it exceeds line or character thresholds.
    /// </summary>
    public static List<string> SplitTextIfNeeded(string text, bool autoSplitEnabled, int maxLines = 4, int maxChars = 220)
    {
        string cleaned = CleanScriptureText(text);
        if (string.IsNullOrWhiteSpace(cleaned)) return new List<string> { string.Empty };

        if (!autoSplitEnabled)
        {
            return new List<string> { cleaned };
        }

        // Split by explicit line breaks first (useful for hymns/stanzas)
        var explicitLines = cleaned.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Smart Thresholding: If line count <= maxLines AND char count <= maxChars + 40, return as single slide
        if (explicitLines.Length <= maxLines && cleaned.Length <= maxChars + 40)
        {
            return new List<string> { cleaned };
        }

        List<string> chunks = new();

        if (explicitLines.Length > 1)
        {
            // Hymn / multi-line verse splitting
            int currentLineCount = 0;
            List<string> currentChunkLines = new();

            foreach (var line in explicitLines)
            {
                currentChunkLines.Add(line.Trim());
                currentLineCount++;

                if (currentLineCount >= maxLines || string.Join("\n", currentChunkLines).Length >= maxChars)
                {
                    chunks.Add(string.Join("\n", currentChunkLines));
                    currentChunkLines.Clear();
                    currentLineCount = 0;
                }
            }

            if (currentChunkLines.Count > 0)
            {
                chunks.Add(string.Join("\n", currentChunkLines));
            }
        }
        else
        {
            // Paragraph / single continuous verse splitting at punctuation (., ?, !, ;, :)
            var clauses = Regex.Split(cleaned, @"(?<=[.?!;:])\s+");
            List<string> currentClauseList = new();
            int currentLength = 0;

            foreach (var clause in clauses)
            {
                if (currentLength + clause.Length > maxChars && currentClauseList.Count > 0)
                {
                    chunks.Add(string.Join(" ", currentClauseList));
                    currentClauseList.Clear();
                    currentLength = 0;
                }

                currentClauseList.Add(clause);
                currentLength += clause.Length + 1;
            }

            if (currentClauseList.Count > 0)
            {
                chunks.Add(string.Join(" ", currentClauseList));
            }
        }

        return chunks.Count > 0 ? chunks : new List<string> { cleaned };
    }
}
