using System.Text.Json;
using System.Text.RegularExpressions;

namespace ALDXVKSCU
{
    public static class Program
    {
        private static HttpClient httpClient = new();

        private static string mergeToolCommand = "dxvk-cache-tool";
        private static string postJsonUrl = "https://www.reddit.com/r/linux_gaming/comments/t5xrho/.json";
        private static string outputFile = "output.dxvk-cache";
        private static string idHistoryFile = "ids.list";
        private static string inputFile1 = "r5apex.dxvk-cache";
        private static string inputFile2 = "r5apex.dxvk-cache";

        internal static async Task Main(string[] args)
        {
            var link = await GetPostText();

            if (!File.Exists(idHistoryFile))
            {
                FileStream idfs = File.Create(idHistoryFile);
                idfs.Close();
            }

            string[] ids = await File.ReadAllLinesAsync(idHistoryFile);

            if (!ids.Contains(link.Id1 + "-" + link.Id2))
            {
                string downloadFilename = "r5apex." + DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm") + ".dxvk-cache";
                using Stream stream = await httpClient.GetStreamAsync(link.Link);
                using FileStream fs = File.OpenWrite(downloadFilename);
                await stream.CopyToAsync(fs);
                await File.AppendAllTextAsync(idHistoryFile, link.Id1 + "-" + link.Id2);

                File.Copy(downloadFilename, "r5apex.dxvk-cache", true);
            }
        }

        private static async Task<(string Link, string Id1, string Id2)> GetPostText()
        {
            string response = await httpClient.GetStringAsync(postJsonUrl);
            JsonDocument doc = JsonDocument.Parse(response);

            JsonElement postJson = doc.RootElement.EnumerateArray().First();
            string postText = postJson.GetProperty("data").GetProperty("children").EnumerateArray().First().GetProperty("data").GetProperty("selftext").GetString()!;

            Match currentLinkMatch = Regex.Match(postText, @"https:\/\/cdn\.discordapp.com\/attachments\/(?<id1>\d+)\/(?<id2>\d+)\/r5apex.dxvk-cache");

            return (currentLinkMatch.Value, currentLinkMatch.Groups["id1"].Value, currentLinkMatch.Groups["id2"].Value);
        }
    }
}






