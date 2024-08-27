using NAudio.Wave;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;

namespace BarkAPI
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string url = "http://localhost:8080/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine($"Listening for requests at {url}");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                Console.WriteLine($"Received request for {request.Url}");

                if (request.Url.AbsolutePath == "/generate-audio")
                {

                    string voice_preset = HttpUtility.ParseQueryString(request.Url.Query).Get("voice_preset") ?? "v2/en_speaker_9";
                    string text = HttpUtility.ParseQueryString(request.Url.Query).Get("text") ?? "Hello";

                    // Generate a WAV file based on the provided text
                    byte[] wavData = await GenerateWavFileFromTextAsync(text, voice_preset);

                    // Set the response MIME type to audio/wav
                    response.ContentType = "audio/wav";
                    response.ContentLength64 = wavData.Length;

                    // Send the WAV file to the client
                    await response.OutputStream.WriteAsync(wavData, 0, wavData.Length);
                }
                else
                {
                    string responseString = "<html><body>Hello. Wrong endpoint.</body></html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }

                response.OutputStream.Close();
            }
        }

        static string QuoteArgument(string arg)
        {
            return $"\"{arg.Replace("\"", "\\\"")}\"";
        }

        static async Task<byte[]> GenerateWavFileFromTextAsync(string text, string voice_preset = "v2/en_speaker_9", string outputFilename = "output.wav")
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                string pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "generate.py");
                var start = new ProcessStartInfo
                {
                    FileName = "python", // Assuming Python is in your PATH
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                start.ArgumentList.Add(pythonScriptPath);
                start.ArgumentList.Add(text);
                start.ArgumentList.Add(outputFilename);

                // Start the process
                using (var process = Process.Start(start))
                {
                    // Read the output stream as it becomes available
                    Task outputTask = Task.Run(async () =>
                    {
                        while (!process.StandardOutput.EndOfStream)
                        {
                            string line = await process.StandardOutput.ReadLineAsync();
                            Console.WriteLine(line);
                        }
                    });

                    // Read the error stream as it becomes available
                    Task errorTask = Task.Run(async () =>
                    {
                        while (!process.StandardError.EndOfStream)
                        {
                            string line = await process.StandardError.ReadLineAsync();
                            Console.WriteLine($"Error: {line}");
                        }
                    });

                    // Wait for the process to finish
                    await process.WaitForExitAsync();

                    // Wait for the output and error tasks to complete
                    await Task.WhenAll(outputTask, errorTask);

                    // Check for errors
                    if (process.ExitCode != 0)
                    {
                        throw new Exception("Python script failed. See the error logs for more details.");
                    }
                }

                return File.ReadAllBytes(outputFilename);
            }
        }
    }
}
