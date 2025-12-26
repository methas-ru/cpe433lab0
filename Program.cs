using System.Text.RegularExpressions;

namespace simple_crawler;

/// <summary>
/// Class <c>Crawler</c> accesses a webpage based on the given URL,
/// stores its content, and recursively accesses linked pages.
/// </summary>
public partial class Crawler
{
    protected string? basedFolder = null;
    protected int maxLinksPerPage = 3;

    // Keep track of visited URLs to avoid duplicate downloads
    protected static HashSet<string> visited = new();

    /// <summary>
    /// Sets the base folder for storing downloaded pages.
    /// </summary>
    public void SetBasedFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder))
            throw new ArgumentNullException(nameof(folder));

        basedFolder = folder;
    }

    /// <summary>
    /// Sets the maximum number of links to follow per page.
    /// </summary>
    public void SetMaxLinksPerPage(int max)
    {
        maxLinksPerPage = max;
    }

    /// <summary>
    /// Downloads a webpage and recursively downloads linked pages.
    /// </summary>
    public async Task GetPage(string url, int level)
    {
        // Base case for recursion
        if (level <= 0)
            return;

        if (basedFolder == null)
            throw new Exception("Please set the base folder first.");

        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));

        // Avoid visiting the same URL multiple times
        if (visited.Contains(url))
            return;

        visited.Add(url);

        HttpClient client = new();

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convert URL to a valid filename
                string fileName = url.Replace(":", "_")
                                     .Replace("/", "_")
                                     .Replace(".", "_") + ".html";

                File.WriteAllText(Path.Combine(basedFolder, fileName), responseBody);

                ISet<string> links = GetLinksFromPage(responseBody);

                int count = 0;
                foreach (string link in links)
                {
                    if (link.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Recursive call
                        await GetPage(link, level - 1);

                        if (++count >= maxLinksPerPage)
                            break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Cannot load content: {0}", response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("Request error: {0}", ex.Message);
        }
    }

    // Regular expression for extracting href links
    [GeneratedRegex("(?<=<a\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))")]
    private static partial Regex MyRegex();

    /// <summary>
    /// Extracts links from HTML content.
    /// </summary>
    public static ISet<string> GetLinksFromPage(string content)
    {
        Regex regexLink = MyRegex();
        HashSet<string> newLinks = [];

        foreach (var match in regexLink.Matches(content))
        {
            string? link = match.ToString();
            if (!string.IsNullOrEmpty(link))
                newLinks.Add(link);
        }

        return newLinks;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Crawler crawler = new();

        crawler.SetBasedFolder(".");
        crawler.SetMaxLinksPerPage(5);

        await crawler.GetPage("https://dandadan.net/", 2);

        Console.WriteLine("Crawling completed.");
    }
}
