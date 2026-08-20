# TensorStack.Media.Windows
`TensorStack.Media.Windows` provides Windows-specific support for reading and writing audio using FFmpeg. It allows loading audio information, reading and writing audio tensors, and muxing audio into video files.
provides Windows-specific support for reading and writing video. It facilitates efficient access to video streams, enabling frames to be read, processed, and saved back to common formats using the Windows media stack.

---

## AudioInput
`AudioInput` wraps an audio file into a convenient object backed by an `AudioTensor`. It allows loading, manipulating, and saving audio.

```csharp
// Load Audio

AudioInput audioInput = new AudioInput("speech.wav");

AudioInput audioInputAsync = await AudioInput.CreateAsync("speech.wav");


// With custom codec, sample rate, and channels

AudioInput audioInput = new AudioInput("music.mp3", "pcm_f32le", 44100, 2);

AudioInput audioInputAsync = await AudioInput.CreateAsync("music.mp3", "pcm_f32le", 44100, 2);


// Save Audio

audioInput.Save("output.wav");

await audioInput.SaveAsync("output.wav");
```



## Notes

* `AudioInput` uses `AudioManager` internally to handle the audio tensor.
* `AudioManager.Initialize` only needed for custom FFmpeg/FFprobe binaries or a different temp directory; the NuGet package provides defaults.

---

# AudioManager
`AudioManager` is a static helper class for loading, saving, and processing audio files. It can read audio into tensors, write tensors back to audio files, extract audio from videos, and add audio to videos. It uses FFmpeg/FFprobe under the hood and provides both synchronous and asynchronous methods.

## Load Audio Information
```csharp
AudioInfo info = AudioManager.LoadInfo("file.wav");

AudioInfo info = await AudioManager.LoadInfoAsync("file.wav");
```

Returns metadata including codec, sample rate, channels, duration, and sample count.

---

## Load Audio Tensor
```csharp
AudioTensor tensor = AudioManager.LoadTensor("file.wav", "pcm_s16le", 16000, 1);

AudioTensor tensor = await AudioManager.LoadTensorAsync("file.wav", "pcm_s16le", 16000, 1);
```

`AudioTensor` contains the raw audio samples in float32 format.

---

## Save Audio Tensor
```csharp
AudioManager.SaveAudio("output.wav", tensor);

await AudioManager.SaveAudioAsync("output.wav", tensor);
```

---

## Add Audio to Video
```csharp
AudioManager.AddAudio("video.mp4", "sourceAudio.mp3");

await AudioManager.AddAudioAsync("video.mp4", "sourceAudio.mp3");
```

This muxes the audio from the source file into the target video.

---
## Initialization

The NuGet package supplies FFmpeg binaries. Initialization is only needed if you want to use custom binaries or a different location:

```csharp
AudioManager.Initialize("ffmpeg.exe", "ffprobe.exe", "Temp");
```

This sets up the executable paths and temporary directory used for conversions.

---
## Notes

* All audio I/O uses FFmpeg under the hood.
* Asynchronous methods use `Task` and support cancellation.
* Audio data is handled in `float32` format internally.

---


## Frame Interpolation
The Interpolation Pipeline uses **RIFE (Real-Time Intermediate Flow Estimation)**
RIFE analyzes motion between consecutive frames and predicts new intermediate frames, producing smoother motion and higher frame rates without traditional frame blending artifacts.  
It’s designed for both speed and quality, making it ideal for upscaling or enhancing AI-generated and low-FPS video content.

## Quick Start

This minimal example demonstrates how to perform **video frame interpolation** using `TensorStack.Media.Windows`.

```csharp
[nuget: TensorStack.Media.Windows]
[nuget: TensorStack.Providers.DML]

async Task QuickStartAsync()
{
    var provider = Provider.GetProvider();

    // Create the interpolation pipeline
    using (var pipeline = InterpolationPipeline.Create(provider))
    {
        // Read video stream
        var inputStream = new VideoInputStream("Input.mp4");

        // Interpolate the stream (e.g., 3x frame rate)
        var outputStream = pipeline.RunAsync(new InterpolationStreamOptions
        {
            Multiplier = 3,
            Stream = inputStream.GetAsync()
        });

        // Save the output video
        await outputStream.SaveAsync("Output.mp4");
    }
}
```

---


- **`Multiplier`** — Defines how many new frames are generated between existing ones.  
  For example, a value of `3` triples the frame rate (turning 30 FPS into 90 FPS).  