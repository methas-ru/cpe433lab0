using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace simple_crawler
{
    /// <summary>
    /// Class <c>Crawler</c> access a webpage based on the given url, then retrieve content
    /// of that webpage and recursively access to linked pages from that web page
    /// </summary>
    public partial class Crawler
    {
        protected string? basedFolder = null;
        protected int maxLinksPerPage = 3;

        // Keep a set of visited URLs to avoid revisiting the same page
        protected HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);

        // Reuse a single HttpClient instance
        private static readonly HttpClient client = new();

        /// <summary>
        /// Method <c>SetBasedFolder</c> sets based folder to store retrieved contents.
        /// </summary>
        /// <param name="folder">the name of the based folder</param>
        public void SetBasedFolder(string folder)
        {
            if (String.IsNullOrEmpty(folder))
            {
                throw new ArgumentNullException(nameof(folder));
            }
            basedFolder = folder;
        }

        /// <summary>
        /// Method <c>SetMaxLinkPerPage</c> sets the maximum number of links that will be recurviely access from a page
        /// </summary>
        /// <param name="max">the maximum number of links</param>
        public void SetMaxLinksPerPage(int max)
        {
            maxLinksPerPage = max;
        }

        /// <summary>
        /// Method <c>GetPage</c> gets a web page based on the url, then recursively access the links in the web page
        /// to get the linked pages.
        /// </summary>
        /// <param name="url">the URL of the webpage to retreive</param>
        /// <param name="level">the number of level to recursively access to</param>
        public async Task GetPage(string url, int level)
        {
            if (basedFolder == null)
            {
                throw new Exception("Please set the value of base folder using SetBasedFolder method first.");
            }
            if (String.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            // Stop recursion when level is zero or below
            if (level <= 0)
            {
                return;
            }

            // Avoid repeated downloads
            if (visited.Contains(url))
            {
                return;
            }
            visited.Add(url);

            // Ensure output folder exists
            Directory.CreateDirectory(basedFolder);

            try
            {
                // Get content from url
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Create a safe filename for the URL
                    string safeName = Regex.Replace(url, @"[^\w\-\.]", "_");
                    string fileName = safeName + ".html";

                    // Store content in file
                    string fullPath = Path.Combine(basedFolder, fileName);
                    File.WriteAllText(fullPath, responseBody);
                    Console.WriteLine($"Saved {url} -> {fullPath}");

                    // Get list of links from content
                    ISet<string> links = GetLinksFromPage(responseBody);

                    int count = 0;
                    // For each link, let's recursive!!!
                    foreach (string link in links)
                    {
                        // We only interested in http/https link (resolve relative links too)
                        string absoluteLink = link;
                        if (!absoluteLink.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                        {
                            try
                            {
                                // Try to resolve relative URL against current page URL
                                Uri baseUri = new Uri(url);
                                Uri combined = new Uri(baseUri, absoluteLink);
                                absoluteLink = combined.AbsoluteUri;
                            }
                            catch
                            {
                                // If URI can't be resolved, skip it
                                continue;
                            }
                        }

                        if (absoluteLink.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // limit number of links in the page, otherwise it will load lots of data
                            if (++count > maxLinksPerPage) break;

                            // Recursive call with decreased level
                            try
                            {
                                await GetPage(absoluteLink, level - 1);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to crawl {absoluteLink}: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Can't load content with return status {0}", response.StatusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("\nException caught:");
                Console.WriteLine("Message :{0}", ex.Message);
            }
        }

        // Template for regular express to extract links
        [GeneratedRegex("(?<=<a\\s*?href=(?:'|\") )[^'\"]*?(?=(?:'|\"))", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex MyRegex();

        /// <summary>
        /// Method <c>GetLInksFromPage</c> extracts links (i.e., <a href="link">...</a>) from web content.
        /// </summary>
        /// <param name="content">HTML page that will be processed to extract links</param>
        public static ISet<string> GetLinksFromPage(string content)
        {
            Regex regexLink = MyRegex();

            HashSet<string> newLinks = new(StringComparer.OrdinalIgnoreCase);
            // We apply regular expression to find matches
            foreach (System.Text.RegularExpressions.Match match in regexLink.Matches(content))
            {
                // For each match, add to hashset (why set? why not list?)
                string? mString = match.Value;
                if (String.IsNullOrEmpty(mString))
                {
                    continue;
                }
                newLinks.Add(mString);
            }
            return newLinks;
        }
    }

    class Program
    {
        // Improved Main: async, accepts simple args: [startUrl] [depth] [maxLinksPerPage] [outputFolder]
        // Example: dotnet run -- https://dandadan.net/ 2 5 ./output
        static async Task Main(string[] args)
        {
            string startUrl = args.Length >= 1 ? args[0] : "https://dandadan.net/";
            int depth = args.Length >= 2 && int.TryParse(args[1], out var d) ? Math.Max(0, d) : 2;
            int maxLinks = args.Length >= 3 && int.TryParse(args[2], out var m) ? Math.Max(1, m) : 5;
            string outFolder = args.Length >= 4 ? args[3] : ".";

            Crawler cw = new();
            cw.SetBasedFolder(outFolder);
            cw.SetMaxLinksPerPage(maxLinks);

            Console.WriteLine($"Starting crawl: {startUrl} (depth={depth}, maxLinksPerPage={maxLinks}, out={outFolder})");

            await cw.GetPage(startUrl, depth);
            Console.WriteLine("Crawl finished.");
        }
    }
}