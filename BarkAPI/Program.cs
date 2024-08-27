using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace BarkAPI
{
    internal class Program
    {        
        /// <summary>
        /// Asynchronously starts the HTTP server and listens for incoming requests.
        /// Handles requests to the "/generate-audio" endpoint with a POST method.
        /// Reads a JSON payload from the request body, deserializes it to an object,
        /// generates audio using the provided text and voice preset, and sends the audio
        /// as a response.
        /// Handles requests to any other endpoint by sending a response with an error
        /// message.
        private static async Task Main(string[] args)
        {
            // Configure logging
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Start the HTTP server
            string url = "http://localhost:8080/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Trace.TraceInformation($"Listening for requests at {url}");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                Trace.TraceInformation($"Received request for {request.Url}");

                if (request.Url.AbsolutePath == "/generate-audio" && request.HttpMethod == "POST")
                {
                    // Read the JSON payload from the request body
                    string requestBody;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        requestBody = await reader.ReadToEndAsync();
                    }

                    // Deserialize the JSON payload to an object
                    dynamic payload = JsonConvert.DeserializeObject(requestBody);
                    string text = payload?.text ?? "Hello";
                    string voicePreset = payload?.voice_preset ?? "v2/en_speaker_9";
                    string outputFilename = "output.wav";

                    Trace.TraceInformation($"Generating audio for text: {text} with voice preset: {voicePreset}");
                    string result = GenerateTTSWithExternalScript(text, voicePreset, outputFilename);

                    if (File.Exists(outputFilename))
                    {
                        byte[] wavData = File.ReadAllBytes(outputFilename);
                        response.ContentType = "audio/wav";
                        response.ContentLength64 = wavData.Length;
                        response.StatusCode = 200;
                        await response.OutputStream.WriteAsync(wavData, 0, wavData.Length);
                        Trace.TraceInformation($"Request completed. Sent {wavData.Length} bytes.");
                        File.Delete(outputFilename);
                    }
                    else
                    {
                        response.StatusCode = 500;
                        Trace.TraceError("Failed to generate the audio file.");
                    }
                }
                else
                {
                    string responseString = "<html><body>Hello. Wrong endpoint.</body></html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    Trace.TraceWarning("Received request for an unknown endpoint.");
                }

                response.OutputStream.Close();
            }
        }
        /// <summary>
        /// Generates text-to-speech (TTS) audio using an external Python script.
        /// </summary>
        /// <param name="message">The text to be converted to TTS audio.</param>
        /// <param name="voicePreset">The voice preset to be used for TTS audio generation.</param>
        /// <param name="outputFilename">The filename where the generated audio will be saved.</param>
        /// <returns>The output of the Python script, which includes any error messages if the script fails.</returns>
        private static string GenerateTTSWithExternalScript(string message, string voicePreset, string outputFilename)
        {
            Trace.TraceInformation("Running Python script to generate TTS...");

            // Define the path to the Python script
            string pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "generate.py");

            // Create the process start info
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = "python", // Path to Python executable, or just "python" if it's in the PATH
                Arguments = $"\"{pythonScriptPath}\" \"{message}\" \"{outputFilename}\" --voice_preset \"{voicePreset}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Start the process
            using (Process process = Process.Start(start))
            {
                StringBuilder output = new StringBuilder();

                // Capture the standard output
                process.OutputDataReceived += (sender, args) => output.AppendLine(args.Data);
                process.BeginOutputReadLine();

                // Capture the standard error
                process.ErrorDataReceived += (sender, args) => output.AppendLine(args.Data);
                process.BeginErrorReadLine();

                // Wait for the process to exit
                process.WaitForExit();

                Trace.TraceInformation("Python script execution completed.");

                // Check if the script encountered any errors
                if (process.ExitCode != 0)
                {
                    Trace.TraceError($"Python script failed: {output.ToString()}");
                }

                return output.ToString();
            }
        }
    }
}