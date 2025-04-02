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
            List<(string url, string folder)> downloadQueue = new List<(string, string)>(); // Queue for URLs and folders

            while (true)
            {
                Console.WriteLine("Enter the folder name inside 'canciones' where this download should be saved:");
                string subfolder = Console.ReadLine();
                string targetFolder = Path.Combine("canciones", subfolder);
                Directory.CreateDirectory(targetFolder); // Ensure the target directory exists

                Console.WriteLine("Enter YouTube video or playlist URL:");
                string url = Console.ReadLine();
                downloadQueue.Add((url, targetFolder));

                Console.WriteLine("Would you like to add another video/playlist? (y/n)");
                string response = Console.ReadLine().Trim().ToLower();
                if (response != "y") break;
            }

            // Path to yt-dlp.exe
            string ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "yt", "yt-dlp.exe");
            if (!File.Exists(ytDlpPath))
            {
                Console.WriteLine($"Error: yt-dlp.exe not found at: {ytDlpPath}");
                return;
            }

            // Process the download queue
            foreach (var (url, targetFolder) in downloadQueue)
            {
                try
                {
                    string sanitizedFileName = await GetVideoTitleAsync(url, ytDlpPath);
                    string outputFilePath = Path.Combine(targetFolder, $"{sanitizedFileName}.mp3");

                    if (File.Exists(outputFilePath))
                    {
                        Console.WriteLine($"Skipping (already downloaded): {sanitizedFileName}");
                    }
                    else
                    {
                        Console.WriteLine($"Downloading: {sanitizedFileName} to {targetFolder}");
                        await DownloadWithYtDlpAsync(url, outputFilePath, ytDlpPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {url}: {ex.Message}");
                }
            }

            Console.WriteLine("All downloads complete!");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching title: {ex.Message}");
            }
            return SanitizeFileName(title);
        }

        static async Task DownloadWithYtDlpAsync(string url, string outputFilePath, string ytDlpPath)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = $"-x --audio-format mp3 -o \"{outputFilePath}\" {url}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    await process.StandardOutput.ReadToEndAsync();
                    await process.StandardError.ReadToEndAsync();
                    process.WaitForExit();
                }
                Console.WriteLine($"Downloaded: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading: {ex.Message}");
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
