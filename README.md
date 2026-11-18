# TensorStack

[![Common Badge](https://img.shields.io/nuget/v/TensorStack.Common?color=4bc51e&label=version)](https://www.nuget.org/packages/TensorStack.Common)
![Nuget](https://img.shields.io/nuget/dt/TensorStack.Common?label=Nuget%20Downloads)
[![GitHub last commit](https://img.shields.io/github/last-commit/TensorStack-AI/AmuseAI)](https://github.com/TensorStack-AI/AmuseAI/commits/master/)

A modular .NET SDK for building AI-powered applications on Windows OS.

TensorStack provides reusable components for local AI inference, text generation, image, video and audio generation, media processing, hardware acceleration, and application development through a unified developer experience.

Built for performance and flexibility, TensorStack powers all applications developed by TensorStack-AI, including [**Amuse**](https://github.com/TensorStack-AI/AmuseAI), the official reference application and UI demonstration of the platform.

---

## Features

- Native .NET SDK
- Local AI inference across multiple backends (ONNX Runtime, HuggingFace, stable-diffusion.cpp, llama.cpp, audio.cpp)
- Text generation and LLM integration, with streaming, thinking, tool calls and image/audio input
- Image and video generation
- Music, speech and speech recognition
- Image upscaling and extraction pipelines
- Image, video, and audio processing
- Hardware acceleration for Nvidia, AMD, Intel and CPU
- Python interoperability
- Extensible provider architecture

---

## Backends

Each backend wraps a native or Python runtime behind the same pattern: create a backend for a `VendorType`, initialize it, then load a pipeline.

```csharp
using (var backend = new LlamaCppBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    using (var pipeline = new LlamaCppPipeline(backend))
    {
        await pipeline.LoadAsync(options);
        // Generate
    }
}
```

| Backend | Runtime | Use |
|---------|---------|-----|
| [TensorStack.OnnxRuntime](TensorStack.OnnxRuntime/README.md) | [ONNX Runtime](https://github.com/microsoft/onnxruntime) (DirectML) | Upscaling, extraction, ONNX text generation |
| [TensorStack.HuggingFace](TensorStack.HuggingFace/README.md) | [Diffusers](https://github.com/huggingface/diffusers) & [Transformers](https://github.com/huggingface/transformers) (Python) | Image and video generation |
| [TensorStack.StableDiffusionCpp](TensorStack.StableDiffusionCpp/README.md) | [stable-diffusion.cpp](https://github.com/leejet/stable-diffusion.cpp) | GGUF image and video generation |
| [TensorStack.LlamaCpp](TensorStack.LlamaCpp/README.md) | [llama.cpp](https://github.com/ggml-org/llama.cpp) | GGUF language models, tool calls, image/audio input |
| [TensorStack.AudioCpp](TensorStack.AudioCpp/README.md) | [audio.cpp](https://github.com/0xShug0/audio.cpp) | Music, speech and speech recognition |

---

## Packages

### Core

| Package | Description | Docs | Package |
|---------|-------------|------|---------|
| TensorStack.Common | Tensor types, pipelines and backend base classes | [README](TensorStack.Common/README.md) | [![Common Badge](https://img.shields.io/nuget/v/TensorStack.Common?color=4bc51e&label=TensorStack.Common)](https://www.nuget.org/packages/TensorStack.Common) |
| TensorStack.Media | Image/Audio/Video loading, saving and streaming | [README](TensorStack.Media/README.md) | [![Media Badge](https://img.shields.io/nuget/v/TensorStack.Media?color=4bc51e&label=TensorStack.Media)](https://www.nuget.org/packages/TensorStack.Media) |

### ONNX Runtime

| Package | Description | Docs | Package |
|---------|-------------|------|---------|
| TensorStack.OnnxRuntime | ONNX model and session management | [README](TensorStack.OnnxRuntime/README.md) | [![OnnxRuntime Badge](https://img.shields.io/nuget/v/TensorStack.OnnxRuntime?color=4bc51e&label=TensorStack.OnnxRuntime)](https://www.nuget.org/packages/TensorStack.OnnxRuntime) |
| TensorStack.OnnxRuntime.Vision | Image and video upscaling and extraction | [README](TensorStack.OnnxRuntime.Vision/README.md) | [![Vision Badge](https://img.shields.io/nuget/v/TensorStack.OnnxRuntime.Vision?color=4bc51e&label=TensorStack.OnnxRuntime.Vision)](https://www.nuget.org/packages/TensorStack.OnnxRuntime.Vision) |
| TensorStack.OnnxRuntime.LLM | Text generation | [README](TensorStack.OnnxRuntime.LLM/README.md) | [![LLM Badge](https://img.shields.io/nuget/v/TensorStack.OnnxRuntime.LLM?color=4bc51e&label=TensorStack.OnnxRuntime.LLM)](https://www.nuget.org/packages/TensorStack.OnnxRuntime.LLM) |

### Generation Backends

| Package | Description | Docs | Package |
|---------|-------------|------|---------|
| TensorStack.HuggingFace | .NET → Python interface for Diffusers & Transformers | [README](TensorStack.HuggingFace/README.md) | [![HuggingFace Badge](https://img.shields.io/nuget/v/TensorStack.HuggingFace?color=4bc51e&label=TensorStack.HuggingFace)](https://www.nuget.org/packages/TensorStack.HuggingFace) |
| TensorStack.StableDiffusionCpp | .NET wrapper for stable-diffusion.cpp | [README](TensorStack.StableDiffusionCpp/README.md) | [![StableDiffusionCpp Badge](https://img.shields.io/nuget/v/TensorStack.StableDiffusionCpp?color=4bc51e&label=TensorStack.StableDiffusionCpp)](https://www.nuget.org/packages/TensorStack.StableDiffusionCpp) |
| TensorStack.LlamaCpp | .NET wrapper for llama.cpp | [README](TensorStack.LlamaCpp/README.md) | [![LlamaCpp Badge](https://img.shields.io/nuget/v/TensorStack.LlamaCpp?color=4bc51e&label=TensorStack.LlamaCpp)](https://www.nuget.org/packages/TensorStack.LlamaCpp) |
| TensorStack.AudioCpp | .NET wrapper for audio.cpp | [README](TensorStack.AudioCpp/README.md) | [![AudioCpp Badge](https://img.shields.io/nuget/v/TensorStack.AudioCpp?color=4bc51e&label=TensorStack.AudioCpp)](https://www.nuget.org/packages/TensorStack.AudioCpp) |

### Application

| Package | Description | Docs | Package |
|---------|-------------|------|---------|
| TensorStack.WPF | WPF controls, services and application host | [README](TensorStack.WPF/README.md) | [![WPF Badge](https://img.shields.io/nuget/v/TensorStack.WPF?color=4bc51e&label=TensorStack.WPF)](https://www.nuget.org/packages/TensorStack.WPF) |

---

## Requirements

- Windows 10 (1809) or later, x64
- .NET 10 (`net10.0-windows10.0.17763.0`)

---

### External Dependencies
- `ONNX Runtime` https://github.com/microsoft/onnxruntime
- `FFMPEG` https://github.com/FFmpeg/FFmpeg
- `Serilog` https://github.com/serilog/serilog
- `CSnakes` https://github.com/tonybaloney/CSnakes
- `SkiaSharp` https://github.com/mono/SkiaSharp
- `OpenCvSharp` https://github.com/shimat/opencvsharp
- `Diffusers` https://github.com/huggingface/diffusers
- `Transformers` https://github.com/huggingface/transformers
- `stable-diffusion.cpp` https://github.com/leejet/stable-diffusion.cpp
- `llama.cpp` https://github.com/ggml-org/llama.cpp
- `audio.cpp` https://github.com/0xShug0/audio.cpp
