using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace GetLit
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the story identifier:");
            string story = Console.ReadLine();

            await ScrapeStoryContent(story);
        }

        static async Task ScrapeStoryContent(string story)
        {
            using (HttpClient client = new HttpClient())
            {
                int page = 1;
                bool continueScraping = true;

                while (continueScraping)
                {
                    string url = $"https://www.literotica.com/s/{story}?page={page}";

                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string html = await response.Content.ReadAsStringAsync();
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(html);

                            // Extract the content from the <div class="aa_ht">
                            var contentNodes = doc.DocumentNode.SelectNodes("//div[@class='aa_ht']");

                            if (contentNodes != null)
                            {
                                foreach (var node in contentNodes)
                                {
                                    Console.WriteLine(node.InnerText.Trim()); // Or process the content as needed
                                }
                            }

                            page++;
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            continueScraping = false;
                        }
                        else
                        {
                            Console.WriteLine($"Unexpected status code: {response.StatusCode}");
                            continueScraping = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                        continueScraping = false;
                    }
                }

                Console.WriteLine("Scraping finished.");
            }
        }
    }
}
