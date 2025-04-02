using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoutubeMp3Downloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<(string url, string folder)> downloadQueue = new List<(string, string)>();

            while (true)
            {
                Console.WriteLine("\n📂 Enter the folder name inside 'canciones' where this download should be saved:");
                string subfolder = Console.ReadLine();
                string targetFolder = Path.Combine("canciones", subfolder);
                Directory.CreateDirectory(targetFolder);

                Console.WriteLine("🎵 Enter YouTube video or playlist URL:");
                string url = Console.ReadLine();
                downloadQueue.Add((url, targetFolder));

                Console.Write("\n➕ Would you like to add another video/playlist? (y/n): ");
                string response = Console.ReadLine().Trim().ToLower();
                if (response != "y") break;
            }

            string ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "yt", "yt-dlp.exe");
            if (!File.Exists(ytDlpPath))
            {
                Console.WriteLine($"❌ Error: yt-dlp.exe not found at: {ytDlpPath}");
                return;
            }

            Console.WriteLine("\n🚀 Starting downloads...\n");

            foreach (var (url, targetFolder) in downloadQueue)
            {
                try
                {
                    Console.WriteLine($"🔍 Fetching video info for: {url}");
                    bool isPlaylist = url.Contains("list="); // Detect if it's a playlist

                    if (isPlaylist)
                    {
                        Console.WriteLine($"📁 Detected as a playlist. Downloading all videos to {targetFolder}...\n");
                        await DownloadPlaylistWithYtDlpAsync(url, targetFolder, ytDlpPath);
                    }
                    else
                    {
                        string sanitizedFileName = await GetVideoTitleAsync(url, ytDlpPath);
                        string outputFilePath = Path.Combine(targetFolder, $"{sanitizedFileName}.mp3");

                        if (File.Exists(outputFilePath))
                        {
                            Console.WriteLine($"✅ Skipping (already downloaded): {sanitizedFileName}\n");
                        }
                        else
                        {
                            Console.WriteLine($"🎬 Downloading: {sanitizedFileName} to {targetFolder}...\n");
                            await DownloadWithYtDlpAsync(url, outputFilePath, ytDlpPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing {url}: {ex.Message}\n");
                }
            }

            Console.WriteLine("\n✅✅✅ All downloads complete! ✅✅✅");
        }

        static async Task<string> GetVideoTitleAsync(string url, string ytDlpPath)
        {
            string title = "video";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = $"--get-title {url}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    title = await process.StandardOutput.ReadLineAsync();
                    process.WaitForExit();
                }

                Console.WriteLine($"📌 Video title fetched: {title}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching title: {ex.Message}");
            }
            return SanitizeFileName(title);
        }

        static async Task DownloadWithYtDlpAsync(string url, string folder, string ytDlpPath)
{
    try
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = $"-x --audio-format mp3 -o \"{folder}\\%(title)s.%(ext)s\" {url}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = psi })
        {
            process.Start();
            Console.WriteLine("📡 Downloading...");
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            Console.WriteLine(output);
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine($"⚠️ yt-dlp Warning/Error: {error}");

            Console.WriteLine($"✅ Download complete!\n");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error downloading: {ex.Message}");
    }
}


        static async Task DownloadPlaylistWithYtDlpAsync(string url, string folder, string ytDlpPath)
{
    try
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = $"--yes-playlist -x --audio-format mp3 -o \"{folder}\\%(playlist_title)s\\%(title)s.%(ext)s\" {url}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = psi })
        {
            process.Start();
            Console.WriteLine($"📂 Downloading playlist: {url}");

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            Console.WriteLine(output);
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine($"⚠️ yt-dlp Warning/Error: {error}");

            Console.WriteLine($"✅ Playlist download complete!\n");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error downloading playlist: {ex.Message}");
    }
}


        static string SanitizeFileName(string fileName)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegEx = new Regex($"[{invalidChars}]");
            return invalidRegEx.Replace(fileName, "_");
        }
    }
}
