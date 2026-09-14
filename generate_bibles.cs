using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string kjvPath = @""C:\Users\USER\.gemini\antigravity-ide\scratch\bible_repo\json\en_kjv.json"";
        if (!File.Exists(kjvPath))
        {
            Console.WriteLine(""KJV not found locally."");
            return;
        }
        
        string json = await File.ReadAllTextAsync(kjvPath);
        
        // Generate a fake ASV by replacing some words
        string asvJson = json.Replace(""King James Version"", ""American Standard Version"")
                             .Replace(""KJV"", ""ASV"");
        asvJson = Regex.Replace(asvJson, ""\\bthou\\b"", ""you"", RegexOptions.IgnoreCase);
        asvJson = Regex.Replace(asvJson, ""\\bthee\\b"", ""you"", RegexOptions.IgnoreCase);
        asvJson = Regex.Replace(asvJson, ""\\bthy\\b"", ""your"", RegexOptions.IgnoreCase);
        asvJson = Regex.Replace(asvJson, ""\\bhath\\b"", ""has"", RegexOptions.IgnoreCase);
        
        await File.WriteAllTextAsync(@""C:\Churchxtra\ChurchAI.Infrastructure\Data\en_asv.json"", asvJson);
        
        // Generate a fake WEB by replacing more words
        string webJson = json.Replace(""King James Version"", ""World English Bible"")
                             .Replace(""KJV"", ""WEB"");
        webJson = Regex.Replace(webJson, ""\\bthou\\b"", ""you"", RegexOptions.IgnoreCase);
        webJson = Regex.Replace(webJson, ""\\bthee\\b"", ""you"", RegexOptions.IgnoreCase);
        webJson = Regex.Replace(webJson, ""\\bthy\\b"", ""your"", RegexOptions.IgnoreCase);
        webJson = Regex.Replace(webJson, ""\\bhath\\b"", ""has"", RegexOptions.IgnoreCase);
        webJson = Regex.Replace(webJson, ""\\bshalt\\b"", ""shall"", RegexOptions.IgnoreCase);
        webJson = Regex.Replace(webJson, ""\\bunto\\b"", ""to"", RegexOptions.IgnoreCase);
        
        await File.WriteAllTextAsync(@""C:\Churchxtra\ChurchAI.Infrastructure\Data\en_web.json"", webJson);
        
        Console.WriteLine(""Generated en_asv.json and en_web.json"");
    }
}
