using System.Text.RegularExpressions;

namespace simple_crawler;

/// <summary>
/// Class <c>Crawler</c> access a webpage based on the given url, then retrieve content
/// of that webpage and recursively access to linked pages from that web page
/// </summary>
public partial class Crawler
{
    
    protected String? basedFolder = null;
    protected int maxLinksPerPage = 3;

    /// <summary>
    /// Method <c>SetBasedFolder</c> sets based folder to store retrieved contents.
    /// </summary>
    /// <param name="folder">the name of the based folder</param>
    public void SetBasedFolder(String folder)
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
    public async Task GetPage(String url, int level)
    {
        // Your code here
        // Note: you need this step for recursive operation
        if (basedFolder == null)
        {
            throw new Exception("Please set the value of base folder using SetBasedFolder method first.");
        }
        if (String.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException(nameof(url));
        }

        // For simplicity, we will use <c>HttpClient</c> here, but if you want you can try <c>TcpClient</c>
        HttpClient client = new();

        try
        {
            // Get content from url
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                String responseBody = await response.Content.ReadAsStringAsync();
                // Reformat URL to a valid filename
                String fileName = url.Replace(":", "_").Replace("/", "_").Replace(".", "_") + ".html";
                // Store content in file
                File.WriteAllText(basedFolder + "/" + fileName, responseBody);
                // Get list of links from content
                ISet<String> links = GetLinksFromPage(responseBody);
                int count = 0;
                // For each link, let's recursive!!!
                foreach (String link in links)
                {
                    // We only interested in http/https link
                    if(link.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Your code here
                        // Note: It should be recursive operation here

                        // limit number of links in the page, otherwise it will load lots of data
                        if (++count >= maxLinksPerPage) break;
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
    [GeneratedRegex("(?<=<a\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))")]
    private static partial Regex MyRegex();

    /// <summary>
    /// Method <c>GetLInksFromPage</c> extracts links (i.e., <a href="link">...</a>) from web content.
    /// </summary>
    /// <param name="content">HTML page that will be processed to extract links</param>
    public static ISet<string> GetLinksFromPage(string content)
    {
        Regex regexLink = MyRegex();

        HashSet<string> newLinks = [];
        // We apply regular expression to find matches
        foreach (var match in regexLink.Matches(content))
        {
            // For each match, add to hashset (why set? why not list?)
            String? mString = match.ToString();
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
    static void Main(string[] args)
    {
        Crawler cw = new();
        // Can you improve this code?
        cw.SetBasedFolder(".");
        cw.SetMaxLinksPerPage(5);
        cw.GetPage("https://dandadan.net/", 2).Wait();
    }
}
