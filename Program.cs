// Version: 1.0.0
// Last updated: 2025-05-22
/*
    Program.cs - Automatic Edge Browser Web Searcher
    ------------------------------------------------
    This C# console application automates Microsoft Edge browser searches using Selenium WebDriver.
    It loads search queries from an embedded resource, removes duplicates, and performs a specified
    number of random Bing searches. The program simulates human-like typing and delays, and provides
    progress feedback in the console. Useful for automation, testing, or demonstration purposes.
*/

using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Automatic_Edge_Browser_Web_Searcher;

class Program
{
    static readonly string[] LineSeparators = new[] { "\r", "\n" };

    static void Main(string[] args)
    {
        // Load queries from embedded resource
        string[] allQueries;
        var assembly = typeof(Program).Assembly;
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
        Console.WriteLine($"  Automatic Edge Browser Web Searcher");
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
        int searchCount = 5;
        Console.Write("How many searches do you want to perform? (default 5): ");
        var input = Console.ReadLine();
        if (int.TryParse(input, out int userCount) && userCount > 0)
        {
            searchCount = userCount;
        }
        // Only use Selenium to launch Edge after user input
        var options = new EdgeOptions();
        // Suppress EdgeDriver logs (including fallback_task_provider warning)
        options.AddArgument("--log-level=3"); // Only fatal errors
        options.AddArgument("--disable-logging");
        options.AddArgument("--disable-logging-redirect");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-software-rasterizer");
        options.AddArgument("--disable-background-timer-throttling");
        options.AddArgument("--disable-backgrounding-occluded-windows");
        options.AddArgument("--disable-renderer-backgrounding");
        using var driver = new EdgeDriver(options);
        var rand = new Random();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\nStarting {searchCount} random searches...\n");
        Console.ResetColor();
        for (int i = 0; i < searchCount; i++)
        {
            string query = uniqueQueries[rand.Next(uniqueQueries.Count)].TrimEnd(',', '"', ' ');
            driver.Navigate().GoToUrl("https://www.bing.com");
            // Wait at least 4 seconds for the browser to load at the start, then less for subsequent searches
            if (i == 0)
                Thread.Sleep(rand.Next(4000, 5000)); // 4-5 seconds for the first search
            else
                Thread.Sleep(rand.Next(300, 600)); // Faster for subsequent searches
            var searchBox = driver.FindElement(By.Id("sb_form_q"));
            searchBox.Clear();
            foreach (char c in query)
            {
                searchBox.SendKeys(c.ToString());
                Thread.Sleep(rand.Next(20, 60)); // Even faster typing delay 0.02-0.06s
            }
            Thread.Sleep(rand.Next(100, 200));
            searchBox.SendKeys(Keys.Enter);
            // Progress bar and status
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[");
            for (int j = 0; j < searchCount; j++)
            {
                if (j < i + 1)
                    Console.Write("#");
                else
                    Console.Write(".");
            }
            Console.Write("] ");
            Console.ResetColor();
            Console.Write($"({i+1}/{searchCount}) Searched: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(query);
            Console.ResetColor();
            if (i < searchCount - 1)
                Thread.Sleep(rand.Next(2000, 3500)); // Wait 2-3.5s between searches
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nAll searches completed!\n");
        Console.ResetColor();
        driver.Quit();
    }
}
