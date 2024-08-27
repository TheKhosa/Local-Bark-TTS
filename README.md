# BarkAPI TTS HTTP Server

This project provides a simple HTTP server that listens for incoming requests and generates text-to-speech (TTS) audio using the BarkModel. The audio generation is performed by a Python script. The server can handle requests to generate audio files based on provided text and voice preset parameters.

## Prerequisites

### 1. Python Setup
- Ensure Python 3 is installed on your system. You can download it from [python.org](https://www.python.org/downloads/).
- Install the required Python packages by running:
  ```bash
  pip install -r requirements.txt
### How It Works
###  Overview
The C# program starts an HTTP server that listens for requests on http://localhost:8080/. When a POST request is sent to the /generate-audio endpoint with a JSON payload containing the text and voice preset, the server generates an audio file using the external Python script and returns the audio as a WAV file in the response.

## JSON Payload
The server expects a JSON payload with the following structure:
{
    "text": "Your text here",
    "voice_preset": "v2/en_speaker_6"
}

text: The text you want to convert into speech.
voice_preset: (Optional) The voice preset to be used for speech generation. Defaults to "v2/en_speaker_9".

### Example Request
You can test the server by sending a POST request using tools like curl or Postman:

curl -X POST http://localhost:8080/generate-audio \
-H "Content-Type: application/json" \
-d '{"text": "Hello, this is a test.", "voice_preset": "v2/en_speaker_6"}' \
--output output.wav

###  Running the Server
Clone the repository and navigate to the project directory.
Ensure Python 3 and the necessary dependencies are installed:
pip install -r requirements.txt

Run the server using the following command:
dotnet run

The server will start listening on http://localhost:8080/. You can send requests to the /generate-audio endpoint to generate TTS audio.

### Handling Requests
Correct Endpoint (/generate-audio): The server processes the request, runs the Python script, and returns the generated audio as a WAV file.
Unknown Endpoint: If any other endpoint is requested, the server responds with an HTML error message.
