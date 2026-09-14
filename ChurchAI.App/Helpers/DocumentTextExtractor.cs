using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChurchAI.Core.Entities;

namespace ChurchAI.App.Helpers;

public static class DocumentTextExtractor
{
    public static async Task<List<Hymn>> ExtractHymnsFromFileAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".json")
        {
            return await ExtractHymnsFromJsonAsync(filePath);
        }
        if (ext == ".csv")
        {
            return await ExtractHymnsFromCsvAsync(filePath);
        }

        string rawText;
        if (ext == ".pdf")
        {
            rawText = await ExtractTextFromPdfAsync(filePath);
        }
        else
        {
            rawText = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        }

        return ParseTextToHymns(rawText);
    }

    public static async Task<string> ExtractTextFromPdfAsync(string pdfPath)
    {
        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(pdfPath);
            string rawContent = Encoding.Latin1.GetString(bytes);

            var sb = new StringBuilder();

            // Find stream ... endstream blocks in PDF
            int streamIdx = 0;
            while ((streamIdx = rawContent.IndexOf("stream", streamIdx, StringComparison.Ordinal)) != -1)
            {
                int start = streamIdx + 6;
                if (start < rawContent.Length && rawContent[start] == '\r') start++;
                if (start < rawContent.Length && rawContent[start] == '\n') start++;

                int end = rawContent.IndexOf("endstream", start, StringComparison.Ordinal);
                if (end == -1) break;

                int length = end - start;
                byte[] streamBytes = new byte[length];
                Array.Copy(bytes, start, streamBytes, 0, length);

                // Check if stream is FlateDecode compressed
                string headerBeforeStream = rawContent.Substring(Math.Max(0, streamIdx - 300), Math.Min(300, streamIdx));
                string decompressedText;

                if (headerBeforeStream.Contains("FlateDecode"))
                {
                    decompressedText = DecompressZLibStream(streamBytes);
                }
                else
                {
                    decompressedText = Encoding.Latin1.GetString(streamBytes);
                }

                ExtractPdfStreamText(decompressedText, sb);
                streamIdx = end + 9;
            }

            // Fallback: If stream parsing yielded very little text, search raw PDF text
            if (sb.Length < 50)
            {
                ExtractPdfStreamText(rawContent, sb);
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF Extract Exception: {ex.Message}");
            return await File.ReadAllTextAsync(pdfPath, Encoding.Latin1);
        }
    }

    private static string DecompressZLibStream(byte[] compressedBytes)
    {
        try
        {
            int offset = 0;
            if (compressedBytes.Length > 2 && compressedBytes[0] == 0x78)
            {
                offset = 2; // Skip ZLib header
            }

            using var ms = new MemoryStream(compressedBytes, offset, compressedBytes.Length - offset);
            using var deflate = new DeflateStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(deflate, Encoding.Latin1);
            return reader.ReadToEnd();
        }
        catch
        {
            return Encoding.Latin1.GetString(compressedBytes);
        }
    }

    private static void ExtractPdfStreamText(string streamContent, StringBuilder sb)
    {
        // Match PDF text strings: (Text here) Tj or (Text here) TJ
        var tjMatches = Regex.Matches(streamContent, @"\(([^)]+)\)\s*(?:Tj|TJ|'|"")");
        foreach (Match m in tjMatches)
        {
            string txt = m.Groups[1].Value
                .Replace(@"\)", ")")
                .Replace(@"\(", "(")
                .Replace(@"\\", @"\");
            if (!string.IsNullOrWhiteSpace(txt)) sb.AppendLine(txt);
        }

        // Match array TJ strings: [(Text1) 12 (Text2)] TJ
        var arrayTjMatches = Regex.Matches(streamContent, @"\[\s*((?:\([^)]+\)\s*|-?\d+\s*)+)\]\s*TJ");
        foreach (Match m in arrayTjMatches)
        {
            var innerMatches = Regex.Matches(m.Groups[1].Value, @"\(([^)]+)\)");
            foreach (Match im in innerMatches)
            {
                string txt = im.Groups[1].Value
                    .Replace(@"\)", ")")
                    .Replace(@"\(", "(")
                    .Replace(@"\\", @"\");
                sb.Append(txt).Append(' ');
            }
            sb.AppendLine();
        }
    }

    private static async Task<List<Hymn>> ExtractHymnsFromJsonAsync(string jsonPath)
    {
        var hymns = new List<Hymn>();
        var content = await File.ReadAllTextAsync(jsonPath);

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                int num = elem.TryGetProperty("number", out var nProp) && nProp.TryGetInt32(out int n) ? n : hymns.Count + 1;
                string title = elem.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? "" : "";
                string lyrics = elem.TryGetProperty("lyrics", out var lProp) ? lProp.GetString() ?? "" : "";

                if (!string.IsNullOrWhiteSpace(lyrics))
                {
                    hymns.Add(new Hymn { Number = num, Title = title, Lyrics = lyrics });
                }
            }
        }
        return hymns;
    }

    private static async Task<List<Hymn>> ExtractHymnsFromCsvAsync(string csvPath)
    {
        var hymns = new List<Hymn>();
        var lines = await File.ReadAllLinesAsync(csvPath);
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length >= 2 && int.TryParse(parts[0].Trim(), out int num))
            {
                string title = parts.Length >= 3 ? parts[1].Trim() : "";
                string lyrics = parts.Length >= 3 ? parts[2].Trim() : parts[1].Trim();
                hymns.Add(new Hymn { Number = num, Title = title, Lyrics = lyrics });
            }
        }
        return hymns;
    }

    public static List<Hymn> ParseTextToHymns(string rawText)
    {
        var lines = rawText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var hymns = new List<Hymn>();
        Hymn? currentHymn = null;
        var lyricsBuilder = new StringBuilder();

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

            // Detect Hymn Header pattern: "1.", "Hymn 1", or standalone number "1"
            var headerMatch = Regex.Match(trimmed, @"^(?:Hymn\s+)?(\d{1,4})(?:\.|\:|-)?(?:\s+(.*))?$", RegexOptions.IgnoreCase);
            if (headerMatch.Success && int.TryParse(headerMatch.Groups[1].Value, out int num) && num > 0 && num <= 2000)
            {
                if (currentHymn != null)
                {
                    currentHymn.Lyrics = lyricsBuilder.ToString().Trim();
                    currentHymn.Title = ExtractTitleFromLyrics(currentHymn.Lyrics, currentHymn.Title);
                    hymns.Add(currentHymn);
                }

                string inlineTitle = headerMatch.Groups[2].Value.Trim();
                currentHymn = new Hymn
                {
                    Number = num,
                    Title = inlineTitle
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

        if (currentHymn != null)
        {
            currentHymn.Lyrics = lyricsBuilder.ToString().Trim();
            currentHymn.Title = ExtractTitleFromLyrics(currentHymn.Lyrics, currentHymn.Title);
            hymns.Add(currentHymn);
        }

        // Fallback: If no numeric headers were found, parse raw text by paragraph blocks
        if (hymns.Count == 0 && !string.IsNullOrWhiteSpace(rawText))
        {
            var blocks = Regex.Split(rawText, @"(?:\r?\n){2,}");
            int autoNum = 1;
            foreach (var b in blocks)
            {
                var trimmedBlock = b.Trim();
                if (trimmedBlock.Length > 10)
                {
                    string title = ExtractTitleFromLyrics(trimmedBlock, string.Empty);
                    hymns.Add(new Hymn
                    {
                        Number = autoNum++,
                        Title = title,
                        Lyrics = trimmedBlock
                    });
                }
            }
        }

        return hymns;
    }

    private static string ExtractTitleFromLyrics(string lyrics, string existingTitle)
    {
        if (!string.IsNullOrWhiteSpace(existingTitle)) return existingTitle;
        if (string.IsNullOrWhiteSpace(lyrics)) return "Untitled Hymn";

        var firstLine = lyrics.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
        if (string.IsNullOrEmpty(firstLine)) return "Untitled Hymn";

        return firstLine.Length > 60 ? firstLine.Substring(0, 57) + "..." : firstLine;
    }
}
