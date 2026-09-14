using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        string path = @""C:\Churchxtra\ChurchAI.Infrastructure\Data\DatabaseSeeder.cs"";
        string content = File.ReadAllText(path);
        
        string newAsvBlock = @""// Local ASV
                string asvPath = @""""C:\Churchxtra\ChurchAI.Infrastructure\Data\en_asv.json"""";
                if (File.Exists(asvPath))
                {
                    string asvJson = await File.ReadAllTextAsync(asvPath);
                    await SeedTranslationFromJsonAsync(asvJson, """"American Standard Version"""", """"ASV"""");
                }
"";
        string newWebBlock = @""// Local WEB
                string webPath = @""""C:\Churchxtra\ChurchAI.Infrastructure\Data\en_web.json"""";
                if (File.Exists(webPath))
                {
                    string webJson = await File.ReadAllTextAsync(webPath);
                    await SeedTranslationFromJsonAsync(webJson, """"World English Bible"""", """"WEB"""");
                }
"";
        
        content = content.Replace(""await SeedTranslationAsync(httpClient, AsvJsonUrl, \"\"American Standard Version\"\", \"\"ASV\"\");"", newAsvBlock);
        content = content.Replace(""await SeedTranslationAsync(httpClient, WebJsonUrl, \"\"World English Bible\"\", \"\"WEB\"\");"", newWebBlock);
        
        File.WriteAllText(path, content);
    }
}
