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

        Console.Write("Select a profile number: ");

        if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 1 || idx > profiles.Length)

        {

            Console.Error.WriteLine("Invalid selection. Please run again and choose a valid number.");

            return;

        }

        string selectedProfileDir = profiles[idx - 1];

        string selectedProfileName = Path.GetFileName(selectedProfileDir);

        Console.WriteLine($"\nSelected Edge profile: '{selectedProfileName}'\n");



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
        {            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("No duplicate queries found.");
            Console.ResetColor();
        }

        // Ask for search count

        int searchCount = 5;

        Console.Write("\nHow many searches do you want to perform? (default 5): ");

        var input = Console.ReadLine();

        if (int.TryParse(input, out int userCount) && userCount > 0)

        {

            searchCount = userCount;

        }



        var rand = new Random();

        Console.ForegroundColor = ConsoleColor.Magenta;

        Console.WriteLine($"\nStarting {searchCount} random searches with profile '{selectedProfileName}'...\n");

        Console.ResetColor();



        // Playwright automation using selected Edge profile

        using var playwright = await Playwright.CreateAsync();

        var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(

            userDataDir: selectedProfileDir,

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

}