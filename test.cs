using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var apiKey = ""C:\Users\USER\AppData\Local\ChurchAI\settings.json"";
        var json = System.IO.File.ReadAllText(apiKey);
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        var key = data.GetProperty(""CloudApiKey"").GetString();

        var systemPrompt = @""You are a Bible reference extraction tool. Convert spoken text into a Bible reference.
If it is a general topic, output the most famous verse reference for it. If it is completely unrelated, output NONE.
Do not explain. Output only the reference or NONE.

Examples:
Input: John chapter 3 verse 16
Output: john 3 16
Input: Jesus wept
Output: john 11 35
Input: testing the microphone
Output: NONE";

        var payload = new
        {
            model = "llama-3.1-8b-instant", // Groq fast model
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = "Jesus wept" }
            },
            temperature = 0.0
        };

        using var httpClient = new HttpClient();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(""Bearer"", key);
        request.Content = JsonContent.Create(payload);

        var response = await httpClient.SendAsync(request);
        Console.WriteLine(""Status: "" + response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseBody);
    }
}
