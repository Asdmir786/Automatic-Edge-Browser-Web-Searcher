// Version: 1.3.0
// Last updated: 2025-06-01
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
        // Install Playwright browsers if needed
        Console.WriteLine("Installing Playwright browsers (msedge and chromium) with --force flag...");
        try
        {
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "msedge", "chromium", "--force" });
            if (exitCode != 0)
            {
                Console.WriteLine("Failed to install browsers. Please ensure you have an active internet connection and try again.");
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
                Environment.Exit(exitCode);
            }
            Console.WriteLine("Playwright browsers (msedge and chromium) installation completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during browser installation: {ex.Message}");
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
            Environment.Exit(1);
        }

        // Load queries from embedded resource

        string[] allQueries;

        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("Automatic_Edge_Browser_Web_Searcher.queries.txt"))
        {
            if (stream == null)
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
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
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
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
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            return;
        }
        Console.WriteLine("Available Edge profiles:");
        for (int i = 0; i < profiles.Length; i++)
            Console.WriteLine($"  [{i + 1}] {Path.GetFileName(profiles[i])}");
        // Profile selection loop
        int idx = -1;
        while (true)
        {
            Console.Write("Select a profile number (default 1): ");
            var inputProfile = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(inputProfile))
            {
                idx = 1;
                break;
            }
            if (int.TryParse(inputProfile, out idx) && idx >= 1 && idx <= profiles.Length)
                break;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Invalid selection. Please enter a valid number from the list.");
            Console.ResetColor();
        }
        string selectedProfileDir = profiles[idx - 1];
        string selectedProfileName = Path.GetFileName(selectedProfileDir);
        Console.WriteLine($"\nSelected Edge profile: '{selectedProfileName}'\n");

        // Check if the -temp folder already exists
        string tempProfileDir = selectedProfileDir + "-temp";
        if (!Directory.Exists(tempProfileDir))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nTo avoid file lock issues, a temporary copy of your profile is required.");
            Console.WriteLine($"File Explorer will open with your selected profile highlighted.\n");
            Console.WriteLine($"Please COPY and PASTE the folder, then rename the copy to: {selectedProfileName}-temp");
            Console.WriteLine($"(Example: If your profile is 'Default', create 'Default-temp')\n");
            Console.WriteLine("Press any key to continue...");
            Console.ResetColor();

            // Open File Explorer with the profile folder selected
            var explorerProc = System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{selectedProfileDir}\"");

            while (!Directory.Exists(tempProfileDir))
            {
                Console.WriteLine($"Waiting for '{selectedProfileName}-temp' to appear in the same directory...");
                Console.WriteLine("Press any key to retry, or Ctrl+C to exit.");
                Console.ReadKey(true);
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Found temp profile: '{Path.GetFileName(tempProfileDir)}'. Using it for automation.\n");
            Console.ResetColor();
        }

        // Inspecting contents of the -temp profile folder
        Console.WriteLine("Inspecting contents of the -temp profile folder...");
        var tempProfileContents = Directory.GetFiles(tempProfileDir);
        foreach (var file in tempProfileContents)
        {
            Console.WriteLine(file);
        }

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
        int browserRetryCount = 0;
        const int maxBrowserRetries = 3;
        
        try
        {
            while (browserRetryCount < maxBrowserRetries)
            {
                try
                {
                    using var playwright = await Playwright.CreateAsync();
                    // Use the temp profile directory as the profile for Playwright
                    var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(
                        userDataDir: tempProfileDir,
                        options: new BrowserTypeLaunchPersistentContextOptions
                        {
                            Headless = false, // Enable headless mode
                            Channel = "msedge"
                        });

                    var page = browserContext.Pages.FirstOrDefault() ?? await browserContext.NewPageAsync();


                    var unusedQueries = new List<string>(uniqueQueries);
                    for (int i = 0; i < searchCount; i++)
                    {
                        if (unusedQueries.Count == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("\nNo more unique queries left to use.");
                            Console.ResetColor();
                            break;
                        }
                        int idxQuery = rand.Next(unusedQueries.Count);
                        string query = unusedQueries[idxQuery].TrimEnd(',', '"', ' ');
                        unusedQueries.RemoveAt(idxQuery);

                        Console.WriteLine($"Performing search {i + 1}/{searchCount}: {query}");

                        int retryCount = 0;
                        bool navigationSuccessful = false;

                        while (retryCount < 3 && !navigationSuccessful) {
                            try {
                                await page.GotoAsync("https://www.bing.com", new PageGotoOptions { Timeout = 30000 }); // Navigate to Bing.com with increased timeout
                                navigationSuccessful = true;
                            } catch (Microsoft.Playwright.PlaywrightException ex) {
                                retryCount++;
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"[Retry {retryCount}] Playwright error during navigation: {ex.Message}");
                                Console.ResetColor();
                            } catch (Exception ex) {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Unexpected error during navigation: {ex.Message}");
                                Console.ResetColor();
                                break;
                            }
                        }

                        if (!navigationSuccessful) {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Failed to navigate to Bing.com after {retryCount} retries. Skipping search.");
                            Console.ResetColor();
                            continue;
                        }



                        try {
                            var searchBox = page.Locator("#sb_form_q");
                            await searchBox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 }); // Increased timeout to 30 seconds
                            await searchBox.FillAsync("");
                            await searchBox.FillAsync(query);
                            await Task.Delay(rand.Next(100, 200));
                            await searchBox.PressAsync("Enter");

                            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                            await Task.Delay(3000);
                        } catch (Exception ex) {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Could not interact with Bing search box: {ex.Message}");
                            Console.ResetColor();
                            continue;
                        }

                        await browserContext.CloseAsync();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nAll searches completed successfully!");
                    Console.ResetColor();
                    
                    // If we got here, everything worked, so break out of the retry loop
                    break;
                }
                catch (PlaywrightException ex) when (ex.GetType().Name == "TargetClosedException")
                {
                    browserRetryCount++;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[Browser Retry {browserRetryCount}/{maxBrowserRetries}] Target closed exception: {ex.Message}");
                    Console.WriteLine("Attempting to restart browser session...");
                    Console.ResetColor();
                    
                    if (browserRetryCount >= maxBrowserRetries)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\nFailed to maintain browser session after {maxBrowserRetries} attempts.");
                        Console.WriteLine("Please check your Edge browser installation and profile.");
                        Console.ResetColor();
                    }
                    
                    // Wait a bit before retrying
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\nUnexpected error: {ex.Message}");
                    Console.WriteLine("Browser logs: ");
                    Console.WriteLine(ex.ToString());
                    Console.ResetColor();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn unhandled error occurred during the automation process: {ex.Message}");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
            Console.ResetColor();
        }
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
}