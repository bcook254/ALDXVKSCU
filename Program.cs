using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ALDXVKSCU
{
    public static class Program
    {
        private static HttpClient httpClient = new();

        private static string postJsonUrl = "https://www.reddit.com/r/linux_gaming/comments/t5xrho/.json";
        private static string idHistoryFile = "ids.list";

        internal static async Task Main(string[] args)
        {
            if (!File.Exists(idHistoryFile))
            {
                Console.WriteLine($"{idHistoryFile} not found...creating");
                using (_ = File.Create(idHistoryFile)) { }
            }

            string postData = await GetPostAsync(postJsonUrl);
            var linkData = GetLinkFromPost(postData);

            string[] savedIds = await File.ReadAllLinesAsync(idHistoryFile);
            string fileId = linkData.Id1 + "-" + linkData.Id2;

            if (savedIds.Contains(fileId))
            {
                Console.WriteLine("File already downloaded");
                return;
            }

            Console.Write($"Downloading file with id {fileId}...");
            string downloadFilename = await DownloadFile(linkData.Link);
            Console.WriteLine("done");
            await File.AppendAllTextAsync(idHistoryFile, fileId + Environment.NewLine);

            File.Copy(downloadFilename, "r5apex.dxvk-cache", true);

            CommitNewFile(fileId);

            File.Delete(downloadFilename);
            Console.WriteLine("Done");
        }

        private static async Task<string> GetPostAsync(string url)
        {
            Console.Write("Getting post data...");
            string response = await httpClient.GetStringAsync(url);
            Console.WriteLine("done");
            return response;
        }

        private static (string Link, string Id1, string Id2) GetLinkFromPost(string postData)
        {
            Console.Write("Extracting cache file download link...");
            JsonDocument doc = JsonDocument.Parse(postData);
            JsonElement postElement = doc.RootElement.EnumerateArray().First();
            string selftext = postElement.GetProperty("data").GetProperty("children").EnumerateArray().First().GetProperty("data").GetProperty("selftext").GetString()!;

            Match currentLinkMatch = Regex.Match(selftext, @"https:\/\/cdn\.discordapp.com\/attachments\/(?<id1>\d+)\/(?<id2>\d+)\/r5apex.dxvk-cache");
            Console.WriteLine("done");
            return (currentLinkMatch.Value, currentLinkMatch.Groups["id1"].Value, currentLinkMatch.Groups["id2"].Value);
        }

        private static async Task<string> DownloadFile(string url)
        {
            string downloadFilename = "r5apex." + DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm") + ".dxvk-cache";
            using Stream stream = await httpClient.GetStreamAsync(url);
            using FileStream fs = File.OpenWrite(downloadFilename);
            await stream.CopyToAsync(fs);

            return downloadFilename;
        }

        private static void CommitNewFile(string fileId)
        {
            Console.WriteLine("Adding updated files to git");
            ProcessStartInfo gitAddInfo = new()
            {
                FileName = "git",
                Arguments = "add r5apex.dxvk-cache ids.list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            Process gitAdd = new()
            {
                StartInfo = gitAddInfo
            };
            gitAdd.Start();

            while (!gitAdd.StandardOutput.EndOfStream)
            {
                Console.WriteLine(gitAdd.StandardOutput.ReadLine());
            }
            gitAdd.WaitForExit();

            Console.WriteLine("Commiting new files to git");
            ProcessStartInfo gitCommitInfo = new()
            {
                FileName = "git",
                Arguments = $"commit -m \"{fileId}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            Process gitCommit = new()
            {
                StartInfo = gitCommitInfo
            };
            gitCommit.Start();

            while (!gitCommit.StandardOutput.EndOfStream)
            {
                Console.WriteLine(gitCommit.StandardOutput.ReadLine());
            }
            gitCommit.WaitForExit();

            Console.WriteLine("Pushing new commits to origin");
            ProcessStartInfo gitPushInfo = new()
            {
                FileName = "git",
                Arguments = "push",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            Process gitPush = new()
            {
                StartInfo = gitPushInfo
            };
            gitPush.Start();

            while (!gitPush.StandardOutput.EndOfStream)
            {
                Console.WriteLine(gitPush.StandardOutput.ReadLine());
            }
            gitPush.WaitForExit();
        }
    }
}






