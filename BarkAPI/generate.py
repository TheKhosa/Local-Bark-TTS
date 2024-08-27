import argparse
from transformers import AutoProcessor, BarkModel
from optimum.bettertransformer import BetterTransformer
from scipy.io.wavfile import write
import torch
import numpy as np
import torchvision
import nltk
from bark import SAMPLE_RATE
from concurrent.futures import ThreadPoolExecutor, as_completed

def main(script, output_filename, voice_preset):
    print(f"PyTorch version: {torch.__version__}")
    print(f"Torchvision version: {torchvision.__version__}")
    print(f"CUDA available: {torch.cuda.is_available()}")

    # Set device to CUDA if available
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    # Load processor and model
    processor = AutoProcessor.from_pretrained("suno/bark")
    model = BarkModel.from_pretrained("suno/bark", torch_dtype=torch.float16).to(device)

    # Convert the model to BetterTransformer
    model = BetterTransformer.transform(model)

    # Ensure output filename ends with .wav
    if not output_filename.lower().endswith(".wav"):
        output_filename += ".wav"

    # Split the script into sentences
    nltk.download('punkt')  # Ensure nltk sentence tokenizer is downloaded
    sentences = nltk.sent_tokenize(script)

    # Define the silence duration between sentences
    silence_duration = 0.2  # in seconds
    silence = np.zeros(int(silence_duration * SAMPLE_RATE))  # Quarter second of silence

    def process_sentence(sentence, index):
        # Process the sentence with the processor including the voice preset
        inputs = processor(sentence, voice_preset=voice_preset)
        
        # Move processor outputs to GPU
        inputs = {key: value.to(device) for key, value in inputs.items()}

        # Generate audio using the model
        sentence_audio = model.generate(**inputs)

        # Move the generated audio back to the CPU
        sentence_audio = sentence_audio.cpu().numpy().squeeze()  # Move to CPU and remove batch dimension
        
        return index, sentence_audio

    # Process sentences in parallel
    with ThreadPoolExecutor() as executor:
        futures = [executor.submit(process_sentence, sentence, idx) for idx, sentence in enumerate(sentences)]
        results = sorted([f.result() for f in as_completed(futures)], key=lambda x: x[0])

    # Concatenate all audio pieces into a single array
    final_audio = np.concatenate([audio for _, audio in results] + [silence] * (len(results) - 1))

    # Define the sample rate
    sample_rate = 22050  # Adjust as needed for your audio

    # Write the final audio array to a WAV file
    write(output_filename, sample_rate, final_audio.astype(np.float32))

    print(f"Audio written to {output_filename}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate audio from text using BarkModel.")
    parser.add_argument("script", type=str, help="The script text to convert into speech.")
    parser.add_argument("output_filename", type=str, help="The output filename for the generated WAV file.")
    parser.add_argument("--voice_preset", type=str, default="v2/en_speaker_6", help="The voice preset to use for generating speech (default: v2/en_speaker_6).")

    args = parser.parse_args()

    main(args.script, args.output_filename, args.voice_preset)
