# TensorStack.Media
Loading, saving and streaming of images, audio and video, using SkiaSharp, OpenCvSharp and FFmpeg.

## Nuget Package
[![Media Badge](https://img.shields.io/nuget/v/TensorStack.Media?color=4bc51e&label=TensorStack.Media)](https://www.nuget.org/packages/TensorStack.Media)


## Setup
Video and audio use FFmpeg, set its location once at startup (defaults to `ffmpeg.exe` and `ffprobe.exe` in the working directory)
```csharp
MediaManager.Initialize("ffmpeg.exe", "ffprobe.exe", "Temp");
```

---

## Images

### ImageInput
`ImageInput` extends `ImageTensor` with loading and saving images to disk
```csharp
var image = new ImageInput("Input.png");
var resized = new ImageInput("Input.png", 1024, 1024, ResizeMode.Crop);
await image.SaveAsync("Output.png");

// Any ImageTensor can be saved directly
await imageTensor.SaveAsync("Output.png");
```

---

## Video

### VideoInput
`VideoInput` extends `VideoSequence`, a buffered video with all frames and audio in memory
```csharp
var video = new VideoInput("Input.mp4");
Console.WriteLine($"{video.FrameCount} frames @ {video.FrameRate}fps");
await video.SaveAsync("Output.mp4");
```

### VideoInputStream
`VideoInputStream` is a video backed by a file stream, loading frames on demand so long videos can be processed one frame at a time
```csharp
var stream = await VideoInputStream.CreateAsync("Input.mp4");
await foreach (var frame in stream.GetAsync())
{
    // Process frame
}

// Or chain a pipeline and save the result
await pipeline.RunAsync(new UpscaleStreamOptions { Stream = stream.GetAsync() }).SaveAsync("Output.mp4");
```

### VideoManager
- `LoadVideoInfoAsync` - Video info and thumbnail
- `LoadVideoSequenceAsync` - Load a `VideoSequence`, with optional size and frame rate overrides
- `WriteVideoStreamAsync` - Write a frame stream to a file, with optional frame processor

---

## Audio

### AudioInput
`AudioInput` extends `AudioTensor` with loading and saving audio to disk
```csharp
var audio = await AudioInput.CreateAsync("Input.wav", sampleRate: 16000, channels: 1);
await audio.SaveAsync("Output.wav");

// Any AudioTensor can be saved, resampled or downmixed
await audioTensor.Resample(24000).ToMono().SaveAsync("Output.wav");
```

### AudioInputStream
`AudioInputStream` is audio backed by a file, loading data on demand
- `GetAsync` - Load as `AudioInput` at a sample rate and channel count
- `MoveAsync` / `CopyAsync` - Move or copy the source file
- `CreateAsync` - Open a file

### AudioManager
- `LoadInfoAsync` - Audio stream info
- `LoadTensorAsync` - Load an `AudioTensor`
- `SaveAudio` - Save an `AudioTensor`
- `AddAudioAsync` - Copy the audio track from a source video to a target video
- `CreateAudioTimelineAsync` - Mix an `AudioTimeline` of segments to a file
