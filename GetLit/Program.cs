using HtmlAgilityPack;
using NAudio.Wave;
using Newtonsoft.Json;
using System.Text;

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
                                    // Extract and clean the text
                                    string rawText = node.InnerText.Trim();

                                    // Remove escape characters and other unnecessary characters
                                    string formattedText = rawText.Replace("\n", " ")
                                                                  .Replace("\r", "")
                                                                  .Replace("\t", " ")
                                                                  .Replace("\\", "")
                                                                  .Replace("\"", "")
                                                                  .Replace("\'", "")
                                                                  .Replace("&nbsp;", " ")
                                                                  .Replace("&amp;", "&");

                                    // Optionally, you can perform further cleaning based on your needs

                                    Console.WriteLine(formattedText); // Or process the content as needed

                                    await sendToTTS(formattedText);
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
        static void PlayAudio(Stream audioStream)
        {
            // Use WaveFileReader to read the WAV data from the stream
            using (WaveFileReader waveReader = new WaveFileReader(audioStream))
            {
                using (WaveOutEvent waveOut = new WaveOutEvent())
                {
                    waveOut.Init(waveReader);
                    waveOut.Play();

                    // Wait for playback to finish
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        private static async Task sendToTTS(string message, string voicePreset = "v2/en_speaker_2")
        {
            string url = "http://localhost:8080/generate-audio";

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                try
                {
                    // Create the payload object
                    var payload = new
                    {
                        text = message,
                        voice_preset = voicePreset
                    };

                    // Serialize the payload to JSON
                    string jsonPayload = JsonConvert.SerializeObject(payload);

                    // Create the StringContent to send in the POST request
                    StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    // Send the POST request with the JSON payload
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a stream
                        using (Stream audioStream = await response.Content.ReadAsStreamAsync())
                        {
                            // Play the audio using NAudio
                            PlayAudio(audioStream);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to get audio. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }
    }
}
