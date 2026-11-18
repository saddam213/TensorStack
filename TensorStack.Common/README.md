# TensorStack.Common
`TensorStack.Common` is the core library shared by every TensorStack package.

## Nuget Package
[![Common Badge](https://img.shields.io/nuget/v/TensorStack.Common?color=4bc51e&label=TensorStack.Common)](https://www.nuget.org/packages/TensorStack.Common)

It contains:
- **Tensors** - `Tensor<T>`, `TensorSpan`, and the media tensors `ImageTensor`, `AudioTensor` and `VideoSequence` / `VideoFrame`
- **Media base classes** - `ImageInputBase`, `AudioInputBase`, `VideoInputBase` and the stream bases, implemented by [TensorStack.Media](../TensorStack.Media/README.md)
- **Pipelines** - `IPipeline`, `IRunOptions`, `IRunProgress` and `RunProgress`
- **Backends** - the `Backend` base class, `BackendConfig`, `BackendStatus` and `BackendType` used by the HuggingFace, stable-diffusion.cpp, llama.cpp and audio.cpp backends
- **Devices** - `Device`, `DeviceType` and `VendorType`
- **Vision** - coordinate types (`Coordinate`, `CoordinateBox`, `CoordinateResult`) and CLIP image helpers
- **Common** - `TextInput`, `Normalization` and tensor extension methods
