# TensorStack.OnnxRuntime.Vision
ONNX image and video pipelines for upscaling, frame interpolation, feature extraction (depth, edges etc), background removal and pose detection.

## Nuget Package
[![OnnxRuntime Vision Badge](https://img.shields.io/nuget/v/TensorStack.OnnxRuntime.Vision?color=4bc51e&label=TensorStack.OnnxRuntime.Vision)](https://www.nuget.org/packages/TensorStack.OnnxRuntime.Vision)

Every pipeline takes an image, a buffered video (`VideoSequence`) or a video stream (`IAsyncEnumerable<VideoFrame>`), so long videos can be processed frame by frame with minimal memory use.

Loading and saving media uses [TensorStack.Media](../TensorStack.Media/README.md).

---

## Quick Start
Upscale an image and a video using [RealESR-General-4x](https://huggingface.co/TensorStack/Upscale-amuse)
```csharp
// 1. Create the Upscale Pipeline
using (var pipeline = UpscalePipeline.Create(new UpscalerConfig
{
    ScaleFactor = 4,
    Normalization = Normalization.ZeroToOne,
    ExecutionProvider = Provider.GetProvider(),
    Path = @"M:\Models\RealESR-General-4x\model.onnx"
}))
{
    // 2. Upscale an Image
    var inputImage = new ImageInput("Input.png");
    var upscaledImage = await pipeline.RunAsync(new UpscaleImageOptions
    {
        Image = inputImage
    });
    await upscaledImage.SaveAsync("Output.png");

    // 3. Upscale a Video (Streaming mode)
    var inputStream = await VideoInputStream.CreateAsync("Input.mp4");
    var upscaledVideo = pipeline.RunAsync(new UpscaleStreamOptions
    {
        Stream = inputStream.GetAsync()
    });
    await upscaledVideo.SaveAsync("Output.mp4");

    // 4. Add audio from the source video (optional)
    await AudioManager.AddAudioAsync("Output.mp4", "Input.mp4");
}
```

---

## Upscale

**UpscalerConfig**
- `Path` - Path to the ONNX model
- `ExecutionProvider` - Hardware provider, see [TensorStack.OnnxRuntime](../TensorStack.OnnxRuntime/README.md)
- `ScaleFactor` - Upscale factor (e.g. 2x, 4x)
- `Normalization` / `OutputNormalization` - Input and output value range (`ZeroToOne` or `OneToOne`)
- `Channels` - Model channels (default 3)
- `SampleSize` - Fixed model input size, `0` for dynamic

### Upscale Image
```csharp
var output = await pipeline.RunAsync(new UpscaleImageOptions
{
    Image = new ImageInput("Input.png")
});
await output.SaveAsync("Output.png");
```

### Upscale Video (Buffered)
Buffers all frames in memory. Suitable for short videos, AI-generated content, low-resolution videos or GIFs.
```csharp
var outputVideo = await pipeline.RunAsync(new UpscaleVideoOptions
{
    Video = new VideoInput("Input.gif")
});
await outputVideo.SaveAsync("Output.mp4");
```

### Upscale Video (Stream)
Processes frames one by one for minimal memory usage. Ideal for high-resolution or long videos.
```csharp
var inputStream = await VideoInputStream.CreateAsync("Input.mp4");
var outputStream = pipeline.RunAsync(new UpscaleStreamOptions
{
    Stream = inputStream.GetAsync()
});
await outputStream.SaveAsync("Output.mp4");
```

### Tiling
Tiling processes images and frames in smaller sections instead of all at once, reducing memory use on large images and high-resolution video.
- `IsTileEnabled` - Enable tiling
- `MaxTileSize` - The maximum size of each tile in pixels, smaller tiles use less memory but may take longer
- `TileOverlap` - The number of overlapping pixels between tiles, more overlap prevents visible seams

```csharp
var output = await pipeline.RunAsync(new UpscaleImageOptions
{
    Image = inputImage,
    IsTileEnabled = true,
    MaxTileSize = 512,
    TileOverlap = 16
});
```

### Upscale Models
Some known and tested models:
- [TensorStack/Upscale-amuse](https://huggingface.co/TensorStack/Upscale-amuse)
- [wuminghao/swinir](https://huggingface.co/wuminghao/swinir)
- [rocca/swin-ir-onnx](https://huggingface.co/rocca/swin-ir-onnx)
- [Xenova/swin2SR-classical-sr-x2-64](https://huggingface.co/Xenova/swin2SR-classical-sr-x2-64)
- [Xenova/swin2SR-classical-sr-x4-64](https://huggingface.co/Xenova/swin2SR-classical-sr-x4-64)
- [Neus/GFPGANv1.4](https://huggingface.co/Neus/GFPGANv1.4)

---

## Interpolation
Increases the frame rate of a video by generating frames in between.
```csharp
using (var pipeline = InterpolationPipeline.Create(Provider.GetProvider()))
{
    var inputVideo = new VideoInput("Input.mp4");
    var outputVideo = await pipeline.RunAsync(new InterpolationVideoOptions
    {
        Multiplier = 3,
        Video = inputVideo
    });
    await outputVideo.SaveAsync("Output.mp4");
}
```
`InterpolationPipeline.Create(provider)` loads `Interpolation.onnx` from the working directory, use `InterpolationPipeline.Create(ModelConfig)` for another path.

---

## Extraction
`ExtractorPipeline` runs image-to-image feature models such as depth, edge or line art detection, commonly used as ControlNet inputs.
```csharp
using (var pipeline = ExtractorPipeline.Create(new ExtractorConfig
{
    Normalization = Normalization.ZeroToOne,
    OutputNormalization = Normalization.OneToOne,
    OutputChannels = 1,
    ExecutionProvider = Provider.GetProvider(),
    Path = @"M:\Models\Depth\model.onnx"
}))
{
    var output = await pipeline.RunAsync(new ExtractorImageOptions
    {
        Image = new ImageInput("Input.png")
    });
    await output.SaveAsync("Depth.png");
}
```
`ExtractorVideoOptions` and `ExtractorStreamOptions` process video the same way as upscaling. Other options: `IsInverted`, `MergeInput` and tiling.

---

## Background Removal
`BackgroundPipeline` uses a segmentation model to remove or isolate the background of an image.
```csharp
using (var pipeline = BackgroundPipeline.Create(new ExtractorConfig
{
    ExecutionProvider = Provider.GetProvider(),
    Path = @"M:\Models\Background\model.onnx"
}))
{
    var output = await pipeline.RunAsync(new BackgroundImageOptions
    {
        Mode = BackgroundMode.RemoveBackground,
        Image = new ImageInput("Input.png")
    });
    await output.SaveAsync("Output.png");
}
```

---

## Pose Detection
`PosePipeline` detects body poses and renders them as a pose skeleton image.
```csharp
using (var pipeline = PosePipeline.Create(new ExtractorConfig
{
    ExecutionProvider = Provider.GetProvider(),
    Path = @"M:\Models\Pose\model.onnx"
}))
{
    var output = await pipeline.RunAsync(new PoseImageOptions
    {
        Image = new ImageInput("Input.png"),
        BodyConfidence = 0.4f,
        JointConfidence = 0.1f
    });
    await output.SaveAsync("Pose.png");
}
```
`PoseVideoOptions` and `PoseStreamOptions` process video.

---

## Combining Pipelines
Pipelines can be chained using `IAsyncEnumerable` streams, so a workflow such as upscaling and frame interpolation runs in a **single pass** without storing intermediate results in memory.

In this example the video is upscaled by **4x** and its frame rate is increased by **3x**:
```csharp
var provider = Provider.GetProvider();
var upscaleConfig = new UpscalerConfig
{
    ScaleFactor = 4,
    ExecutionProvider = provider,
    Normalization = Normalization.ZeroToOne,
    Path = @"M:\Models\RealESR-General-4x\model.onnx"
};

using (var upscalePipeline = UpscalePipeline.Create(upscaleConfig))
using (var interpolationPipeline = InterpolationPipeline.Create(provider))
{
    // Read Stream [512 x 512 @ 8fps]
    var videoInput = await VideoInputStream.CreateAsync("Input.mp4");
    var videoStream = videoInput.GetAsync();

    // Upscale Stream
    videoStream = upscalePipeline.RunAsync(new UpscaleStreamOptions
    {
        Stream = videoStream
    });

    // Interpolate Stream
    videoStream = interpolationPipeline.RunAsync(new InterpolationStreamOptions
    {
        Multiplier = 3,
        Stream = videoStream,
        FrameRate = videoInput.FrameRate,
        FrameCount = videoInput.FrameCount
    });

    // Save Stream [2048 x 2048 @ 24fps]
    await videoStream.SaveAsync("Output.mp4");
}
```
