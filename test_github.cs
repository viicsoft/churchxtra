using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add(""User-Agent"", ""C# App"");
        var html = await client.GetStringAsync(""https://github.com/thiagobodruk/bible/tree/master/json"");
        var matches = Regex.Matches(html, @""en_[a-z0-9]+\.json"");
        foreach(Match m in matches) {
            Console.WriteLine(m.Value);
        }
    }
}
