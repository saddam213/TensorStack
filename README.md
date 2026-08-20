# TensorStack

[![Common Badge](https://img.shields.io/nuget/v/TensorStack.Common?color=4bc51e&label=version)](https://www.nuget.org/packages/TensorStack.Common)
![Nuget](https://img.shields.io/nuget/dt/TensorStack.Common?label=Nuget%20Downloads)
[![GitHub last commit](https://img.shields.io/github/last-commit/TensorStack-AI/AmuseAI)](https://github.com/TensorStack-AI/AmuseAI/commits/master/)

A modular .NET SDK for building AI-powered applications.

TensorStack provides reusable components for local AI inference, text generation, image generation, media processing, hardware acceleration, and application development through a unified developer experience.

Built for performance and flexibility, TensorStack powers all applications developed by TensorStack-AI, including [**Amuse**](https://github.com/TensorStack-AI/AmuseAI), the official reference application and UI demonstration of the platform.

---

## Features

- Native .NET SDK
- Local AI inference
- Text generation and LLM integration
- Stable Diffusion image generation
- Image upscaling and extraction pipelines
- Image, video, and audio processing
- Hardware acceleration support
- Python interoperability
- Extensible provider architecture
- Cross-platform development
---

## Packages

### AI & Inference

| Package | Description | Docs | Package |
|---------|-------------|------|---------|
| TensorStack.Common | Shared runtime and utilities | [README](TensorStack.Common/README.md) | [![Common Badge](https://img.shields.io/nuget/v/TensorStack.Common?color=4bc51e&label=TensorStack.Common)](https://www.nuget.org/packages/TensorStack.Common) |
| TensorStack.TextGeneration | Text generation and LLM integrations | [README](TensorStack.TextGeneration/README.md) | [![TextGeneration Badge](https://img.shields.io/nuget/v/TensorStack.TextGeneration?color=4bc51e&label=TensorStack.TextGeneration)](https://www.nuget.org/packages/TensorStack.TextGeneration) |
| TensorStack.Upscaler | AI image upscaling | [README](TensorStack.Common/Upscaler.md) | [![Upscaler Badge](https://img.shields.io/nuget/v/TensorStack.Upscaler?color=4bc51e&label=TensorStack.Upscaler)](https://www.nuget.org/packages/TensorStack.Upscaler) |
| TensorStack.Extractors | Feature extraction and analysis |[README](TensorStack.Common/Extractors.md) | [![Upscaler Badge](https://img.shields.io/nuget/v/TensorStack.Extractors?color=4bc51e&label=TensorStack.Extractors)](https://www.nuget.org/packages/TensorStack.Extractors) |
| TensorStack.Python | Python interoperability | [README](TensorStack.Common/Python.md) | [![Python Badge](https://img.shields.io/nuget/v/TensorStack.Python?color=4bc51e&label=TensorStack.Python)](https://www.nuget.org/packages/TensorStack.Python) |

### Inference Providers

| Package | Description | Docs | Package |
|---------|-------------|------|---------|
| TensorStack.Providers.CPU | CPU execution provider | [README](TensorStack.Providers.CPU/README.md) | [![CPU Badge](https://img.shields.io/nuget/v/TensorStack.Providers.CPU?color=4bc51e&label=TensorStack.Providers.CPU)](https://www.nuget.org/packages/TensorStack.Providers.CPU) |
| TensorStack.Providers.CUDA | NVIDIA CUDA execution provider | [README](TensorStack.Providers.CUDA/README.md) | [![CUDA Badge](https://img.shields.io/nuget/v/TensorStack.Providers.CUDA?color=4bc51e&label=TensorStack.Providers.CUDA)](https://www.nuget.org/packages/TensorStack.Providers.CUDA) |
| TensorStack.Providers.DML | DirectML execution provider | [README](TensorStack.Providers.DML/README.md) | [![DML Badge](https://img.shields.io/nuget/v/TensorStack.Providers.DML?color=4bc51e&label=TensorStack.Providers.DML)](https://www.nuget.org/packages/TensorStack.Providers.DML) |

---

### Media Processing

| Package | Description | Docs | Package |
|---------|-------------|------|---------|
| TensorStack.Media | Shared Image/Audio/Video utilities | [README](TensorStack.Media/README.md) | [![Image Badge](https://img.shields.io/nuget/v/TensorStack.Media?color=4bc51e&label=TensorStack.Media)](https://www.nuget.org/packages/TensorStack.Media) |
| TensorStack.Media.Bitmap | Image ↔ Tensor (`Bitmap`) | [README](TensorStack.Media.Bitmap/README.md) | [![Bitmap Badge](https://img.shields.io/nuget/v/TensorStack.Media.Bitmap?color=4bc51e&label=TensorStack.Media.Bitmap)](https://www.nuget.org/packages/TensorStack.Media.Bitmap) |
| TensorStack.Media.BitmapImage |  Image ↔ Tensor (`BitmapImage`) | [README](TensorStack.Media.BitmapImage/README.md) | [![BitmapImage Badge](https://img.shields.io/nuget/v/TensorStack.Media.BitmapImage?color=4bc51e&label=TensorStack.Media.BitmapImage)](https://www.nuget.org/packages/TensorStack.Media.BitmapImage) |
| TensorStack.Media.SkiaSharp |  Image ↔ Tensor (`SkiaSharp`) | [README](TensorStack.Media.SkiaSharp/README.md) | [![BitmapImage Badge](https://img.shields.io/nuget/v/TensorStack.Media.SkiaSharp?color=4bc51e&label=TensorStack.Media.SkiaSharp)](https://www.nuget.org/packages/TensorStack.Media.SkiaSharp) |
| TensorStack.Media.Windows | Audio/Video ↔ Tensor (`OpenCvSharp4`, `FFMPEG`) | [README](TensorStack.Media.Windows/README.md) | [![Media.Windows Badge](https://img.shields.io/nuget/v/TensorStack.Media.Windows?color=4bc51e&label=TensorStack.Media.Windows)](https://www.nuget.org/packages/TensorStack.Media.Windows) |



---


### External Dependencies
- `FFMPEG` https://github.com/FFmpeg/FFmpeg
- `PdfPig` https://github.com/UglyToad/PdfPig
- `Markdig` https://github.com/xoofx/markdig
- `Serilog` https://github.com/serilog/serilog
- `CSnakes` https://github.com/tonybaloney/CSnakes
- `SkiaSharp` https://github.com/mono/SkiaSharp
- `ZstdSharp` https://github.com/oleg-st/ZstdSharp
- `OpenCvSharp4` https://github.com/shimat/opencvsharp
- `Diffusers` https://github.com/huggingface/diffusers
- `Transformers` https://github.com/huggingface/transformers
- `StableDiffusion.cpp` https://github.com/leejet/stable-diffusion.cpp
