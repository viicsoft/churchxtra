using System;
using System.Linq;
using System.Collections.Generic;

public class Program 
{
    public static void Main() 
    {
        var spokenText = ""Jesus feeds 5000."";
        var normalizedInput = new string(spokenText.ToLowerInvariant().Where(c => !char.IsPunctuation(c)).ToArray()).Trim();
        Console.WriteLine($""Normalized: '{normalizedInput}'"");
        
        var commonTopics = new Dictionary<string, string>
        {
            { ""jesus feeds 5000"", ""matthew 14 15"" },
            { ""jesus feeds five thousand"", ""matthew 14 15"" },
            { ""jesus wept"", ""john 11 35"" },
            { ""creation of the world"", ""genesis 1 1"" }
        };

        if (commonTopics.TryGetValue(normalizedInput, out var knownReference))
        {
            Console.WriteLine($""Match found: {knownReference}"");
        }
        else
        {
            Console.WriteLine(""No match"");
        }
    }
}
