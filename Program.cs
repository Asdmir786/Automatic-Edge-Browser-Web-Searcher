// Version: 1.1.0
// Last updated: 2025-05-26
/*
    Program.cs - Automatic Edge Browser Web Searcher
    ------------------------------------------------
    This C# console application automates Microsoft Edge browser searches using Playwright.
    It loads search queries from an embedded resource, removes duplicates, and performs a specified
    number of random Bing searches. The program simulates human-like typing and delays, and provides
    progress feedback in the console. It now enumerates real Edge user profiles and allows the user
    to pick one for persistent context via Playwright. After each search, it waits for page load
    completion plus an additional 3 seconds before proceeding.
*/

using Microsoft.Playwright;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace Automatic_Edge_Browser_Web_Searcher;

class Program
{
    static readonly string[] LineSeparators = new[] { "\r", "\n" };

    static async Task Main(string[] args)
    {
        

        // Load queries from embedded resource

        string[] allQueries;

        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("Automatic_Edge_Browser_Web_Searcher.queries.txt"))
        {
            if (stream == null)
            {
                Console.WriteLine("Resource 'Automatic_Edge_Browser_Web_Searcher.queries.txt' not found.");
                return;
            }
            using (var reader = new StreamReader(stream))
            {
                allQueries = reader.ReadToEnd()
                    .Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(q => q.Trim().Trim('"'))
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .ToArray();
            }
        }
        var uniqueQueries = allQueries.Distinct().ToList();
        Console.OutputEncoding = Encoding.UTF8;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n═══════════════════════════════════════════════════════════════════");
        Console.WriteLine($"  Automatic Edge Browser Web Searcher (Playwright)");
        Console.WriteLine($"═══════════════════════════════════════════════════════════════════\n");
        Console.ResetColor();
        Console.WriteLine($"Total queries (including duplicates): {allQueries.Length}");
        Console.WriteLine($"Unique queries: {uniqueQueries.Count}\n");
        var duplicateGroups = allQueries.GroupBy(q => q).Where(g => g.Count() > 1).ToList();
        if (duplicateGroups.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Found {duplicateGroups.Count} duplicate queries. Example duplicates:");
            foreach (var group in duplicateGroups.Take(5))
                Console.WriteLine($"  '{group.Key}' appears {group.Count()} times");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("No duplicate queries found.");
            Console.ResetColor();
        }

        // 1️⃣ Enumerate Edge user profiles
        string edgeRoot = GetEdgeUserDataDir();
        if (!Directory.Exists(edgeRoot))
        {
            Console.Error.WriteLine($"Could not find Edge user data at: {edgeRoot}");
            return;
        }
        var profiles = Directory.EnumerateDirectories(edgeRoot)
            .Where(dir =>
            {
                var name = Path.GetFileName(dir);
                return name.Equals("Default", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("Profile", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();
        if (profiles.Length == 0)
        {
            Console.Error.WriteLine("No Edge profiles found under user data directory.");
            return;
        }
        Console.WriteLine("Available Edge profiles:");
        for (int i = 0; i < profiles.Length; i++)
            Console.WriteLine($"  [{i + 1}] {Path.GetFileName(profiles[i])}");
        // Profile selection loop
        int idx = -1;
        while (true)
        {
            Console.Write("Select a profile number: ");
            var inputProfile = Console.ReadLine();
            if (int.TryParse(inputProfile, out idx) && idx >= 1 && idx <= profiles.Length)
                break;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid selection. Please enter a valid number from the list.");
            Console.ResetColor();
        }
        string selectedProfileDir = profiles[idx - 1];
        string selectedProfileName = Path.GetFileName(selectedProfileDir);
        Console.WriteLine($"\nSelected Edge profile: '{selectedProfileName}'\n");



        // Ask for search count (loop until valid)
        int searchCount = 5;
        while (true)
        {
            Console.Write("\nHow many searches do you want to perform? (default 5): ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                searchCount = 5;
                break;
            }
            if (int.TryParse(input, out int userCount) && userCount > 0)
            {
                searchCount = userCount;
                break;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid number. Please enter a positive integer.");
            Console.ResetColor();
        }



        var rand = new Random();

        Console.ForegroundColor = ConsoleColor.Magenta;

        Console.WriteLine($"\nStarting {searchCount} random searches with profile '{selectedProfileName}'...\n");

        Console.ResetColor();



        // Playwright automation using selected Edge profile

        using var playwright = await Playwright.CreateAsync();

        // Copy selected profile to a temp directory to avoid profile lock issues, with retry if locked
        string tempProfileDir = Path.Combine(Path.GetTempPath(), $"EdgeProfile_{Guid.NewGuid()}");
        bool copySuccess = false;
        while (!copySuccess)
        {
            try
            {
                DirectoryCopy(selectedProfileDir, tempProfileDir, true);
                copySuccess = true;
            }
            catch (IOException ex) when (ex.Message.Contains("because it is being used by another process"))
            {
                // Instantly show what is locking the file (if handle.exe is available)
                string lockedFile = ExtractLockedFilePath(ex.Message);
                if (!string.IsNullOrEmpty(lockedFile))
                {
                    try
                    {
                        string handlePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "handle.exe");
                        if (File.Exists(handlePath))
                        {
                            var psi = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = handlePath,
                                Arguments = $"-accepteula \"{lockedFile}\"",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using (var proc = System.Diagnostics.Process.Start(psi))
                            {
                                if (proc != null)
                                {
                                    string output = proc.StandardOutput.ReadToEnd();
                                    proc.WaitForExit();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("\nProcesses locking the file (handle.exe output):\n");
                                    Console.ResetColor();
                                    Console.WriteLine(output);
                                    // Try to automatically kill Edge processes locking the file
                                    var edgePids = new List<int>();
                                    foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        // Typical handle.exe output:  msedge.exe       pid: 1234   ...
                                        if (line.Contains("msedge.exe") && line.Contains("pid:"))
                                        {
                                            var pidIndex = line.IndexOf("pid:");
                                            if (pidIndex != -1)
                                            {
                                                var afterPid = line.Substring(pidIndex + 4).TrimStart();
                                                var pidStr = new string(afterPid.TakeWhile(char.IsDigit).ToArray());
                                                if (int.TryParse(pidStr, out int pid))
                                                    edgePids.Add(pid);
                                            }
                                        }
                                    }
                                    if (edgePids.Count > 0)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Cyan;
                                        Console.WriteLine($"\nAttempting to kill Edge processes locking the file: {string.Join(", ", edgePids)}");
                                        Console.ResetColor();
                                        bool allKilled = true;
                                        foreach (var pid in edgePids.Distinct())
                                        {
                                            try
                                            {
                                                var procToKill = System.Diagnostics.Process.GetProcessById(pid);
                                                procToKill.Kill();
                                                Console.ForegroundColor = ConsoleColor.Green;
                                                Console.WriteLine($"Process {pid} killed successfully.");
                                                Console.ResetColor();
                                            }
                                            catch (Exception killEx)
                                            {
                                                allKilled = false;
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.WriteLine($"Failed to kill process {pid}: {killEx.Message}");
                                                Console.ResetColor();
                                            }
                                        }
                                        if (allKilled)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine("\nAll locking Edge processes killed. Retrying profile copy automatically...\n");
                                            Console.ResetColor();
                                            continue;
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("\nSome processes could not be killed. Please close all Edge browser windows (including background processes) and press Enter to retry.\n");
                                            Console.ResetColor();
                                            Console.ReadLine();
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("No msedge.exe processes found in handle.exe output to kill automatically.");
                                        // Only prompt if handle.exe output actually found a locking process, otherwise just retry
                                        if (output.Contains("No matching handles found."))
                                        {
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine("\nNo processes are locking the file. Retrying profile copy automatically...\n");
                                            Console.ResetColor();
                                            continue;
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("\nA file in the Edge profile is locked. Please close all Edge browser windows (including background processes) and press Enter to retry.\n");
                                            Console.ResetColor();
                                            Console.ReadLine();
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("(Could not start handle.exe process.)");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("(handle.exe not found in application directory. Download from https://docs.microsoft.com/en-us/sysinternals/downloads/handle if you want this feature.)");
                        }
                    }
                    catch
                    {
                        Console.WriteLine("(Could not run handle.exe to show locking process. Download from https://docs.microsoft.com/en-us/sysinternals/downloads/handle if you want this feature.)");
                    }
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nA file in the Edge profile is locked. Please close all Edge browser windows (including background processes) and press Enter to retry.\n");
                Console.ResetColor();
                Console.ReadLine();
            }
        }
        Console.WriteLine($"Copied profile to temp directory: {tempProfileDir}");

        // Use the temp profile directory as the profile for Playwright
        var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(
            userDataDir: tempProfileDir,
            options: new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                Channel = "msedge"
            });

        var page = browserContext.Pages.FirstOrDefault() ?? await browserContext.NewPageAsync();



        for (int i = 0; i < searchCount; i++)

        {

            string query = uniqueQueries[rand.Next(uniqueQueries.Count)].TrimEnd(',', '"', ' ');

            await page.GotoAsync("https://www.bing.com");

            if (i == 0)

                await Task.Delay(rand.Next(4000, 5000));

            else

                await Task.Delay(rand.Next(300, 600));



            var searchBox = page.Locator("#sb_form_q");

            try

            {

                await searchBox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });

                await searchBox.FillAsync("");

                await searchBox.FillAsync(query);

                await Task.Delay(rand.Next(100, 200));

                await searchBox.PressAsync("Enter");

                // Wait for page load and extra time

                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await Task.Delay(3000);

            }

            catch (Exception ex)

            {

                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"Could not interact with Bing search box: {ex.Message}");

                Console.ResetColor();

                continue;

            }



            // Progress bar

            int barWidth = 20;

            double progress = (i + 1) / (double)searchCount;

            int filled = (int)(progress * barWidth);

            string bar = new string('█', filled) + new string('─', barWidth - filled);

            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.Write($"\r[{bar}] {i + 1}/{searchCount} searches");

            Console.ResetColor();

            await Task.Delay(rand.Next(1200, 2000));

        }



        // Cleanup

        await browserContext.CloseAsync();

    }



    static string GetEdgeUserDataDir()

    {

        if (OperatingSystem.IsWindows())

        {

            return Path.Combine(

                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),

                "Microsoft", "Edge", "User Data");

        }

        else if (OperatingSystem.IsMacOS())

        {

            return Path.Combine(

                Environment.GetFolderPath(Environment.SpecialFolder.Personal),

                "Library", "Application Support", "Microsoft Edge");

        }

        else // Linux

        {

            return Path.Combine(

                Environment.GetFolderPath(Environment.SpecialFolder.Personal),

                ".config", "microsoft-edge");

        }

    }

    // Add a helper method to copy directories recursively
    static void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");
        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destDir);
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destDir, file.Name);
            try
            {
                file.CopyTo(targetFilePath, true);
            }
            catch (IOException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[Warning] Could not copy file '{file.FullName}': {ex.Message}");
                    Console.ResetColor();
                    // Let the exception bubble up for retry logic in Main
                    throw;
                }
        }
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string targetSubDir = Path.Combine(destDir, subdir.Name);
                DirectoryCopy(subdir.FullName, targetSubDir, copySubDirs);
            }
        }
    }

    // Helper to extract locked file path from IOException message
    static string ExtractLockedFilePath(string message)
    {
        int firstQuote = message.IndexOf('\'');
        int secondQuote = message.IndexOf('\'', firstQuote + 1);
        if (firstQuote != -1 && secondQuote != -1 && secondQuote > firstQuote)
            return message.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        return string.Empty;
    }
}