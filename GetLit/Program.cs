using HtmlAgilityPack; // Used for parsing and manipulating HTML documents
using NAudio.Wave; // Used for handling audio playback
using Newtonsoft.Json; // Used for JSON serialization and deserialization
using System.Text; // Used for encoding and manipulating text data

// Declaring the namespace for the application
namespace GetLit
{
    // The main class of the program
    internal class Program
    {

        static async Task Main(string[] args)
        {
            // Prompting the user to enter a story identifier
            Console.WriteLine("Enter the story identifier:");
            string story = Console.ReadLine(); // Reading user input

            // Calling the method to scrape story content asynchronously
            await ScrapeStoryContent(story);
        }

        // Method to scrape the content of the story from a website
        static async Task ScrapeStoryContent(string story)
        {
            // Creating an HttpClient instance to send HTTP requests
            using (HttpClient client = new HttpClient())
            {
                int page = 1; // Variable to keep track of the page number
                bool continueScraping = true; // Flag to control the scraping loop

                // Loop to continue scraping until no more pages are found
                while (continueScraping)
                {
                    // Constructing the URL with the story identifier and page number
                    string url = $"https://www.literotica.com/s/{story}?page={page}";

                    try
                    {
                        // Sending an HTTP GET request to the constructed URL
                        HttpResponseMessage response = await client.GetAsync(url);

                        // If the request is successful, proceed with processing the response
                        if (response.IsSuccessStatusCode)
                        {
                            // Reading the HTML content of the response
                            string html = await response.Content.ReadAsStringAsync();
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(html); // Loading the HTML content into an HtmlDocument

                            // Selecting the nodes containing the story content using XPath
                            var contentNodes = doc.DocumentNode.SelectNodes("//div[@class='aa_ht']");

                            // If content nodes are found, process them
                            if (contentNodes != null)
                            {
                                foreach (var node in contentNodes)
                                {
                                    // Extracting and cleaning the text content
                                    string rawText = node.InnerText.Trim();

                                    // Removing escape characters and other unnecessary characters
                                    string formattedText = rawText.Replace("\n", " ")
                                                                  .Replace("\r", "")
                                                                  .Replace("\t", " ")
                                                                  .Replace("\\", "")
                                                                  .Replace("\"", "")
                                                                  .Replace("\'", "")
                                                                  .Replace("&nbsp;", " ")
                                                                  .Replace("&amp;", "&");

                                    // Printing the cleaned content or processing it further
                                    Console.WriteLine(formattedText);

                                    // Sending the formatted text to the TTS (Text-to-Speech) service
                                    await sendToTTS(formattedText);
                                }
                            }

                            // Incrementing the page number to scrape the next page
                            page++;
                        }
                        // If the page is not found (404), stop scraping
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            continueScraping = false;
                        }
                        // If an unexpected status code is received, log it and stop scraping
                        else
                        {
                            Console.WriteLine($"Unexpected status code: {response.StatusCode}");
                            continueScraping = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handling any errors that occur during the HTTP request
                        Console.WriteLine($"An error occurred: {ex.Message}");
                        continueScraping = false; // Stop scraping if an error occurs
                    }
                }

                // Indicating that scraping has finished
                Console.WriteLine("Scraping finished.");
            }
        }

        // Method to play the audio stream using NAudio
        static void PlayAudio(Stream audioStream)
        {
            // Using WaveFileReader to read the WAV data from the audio stream
            using (WaveFileReader waveReader = new WaveFileReader(audioStream))
            {
                using (WaveOutEvent waveOut = new WaveOutEvent())
                {
                    // Initializing the audio output device with the WAV data
                    waveOut.Init(waveReader);
                    waveOut.Play(); // Starting audio playback

                    // Loop to wait until the audio playback is finished
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100); // Pause briefly to prevent CPU overuse
                    }
                }
            }
        }

        // Method to send the text to a TTS (Text-to-Speech) service and play the resulting audio
        private static async Task sendToTTS(string message, string voicePreset = "v2/en_speaker_2")
        {
            // Defining the URL of the TTS service
            string url = "http://localhost:8080/generate-audio";

            // Creating an HttpClient instance for sending the HTTP request
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan; // Setting an infinite timeout for the request

                try
                {
                    // Creating the payload object with the message text and voice preset
                    var payload = new
                    {
                        text = message,
                        voice_preset = voicePreset
                    };

                    // Serializing the payload to a JSON string
                    string jsonPayload = JsonConvert.SerializeObject(payload);

                    // Creating StringContent to send in the POST request with JSON content type
                    StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    // Sending the POST request to the TTS service
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    // If the request is successful, process the response
                    if (response.IsSuccessStatusCode)
                    {
                        // Reading the response content as a stream (audio data)
                        using (Stream audioStream = await response.Content.ReadAsStreamAsync())
                        {
                            // Playing the audio using NAudio
                            PlayAudio(audioStream);
                        }
                    }
                    // If the request fails, log the status code
                    else
                    {
                        Console.WriteLine($"Failed to get audio. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Handling any errors that occur during the request
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }
    }
}
