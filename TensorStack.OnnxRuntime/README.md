# TensorStack.OnnxRuntime
`TensorStack.OnnxRuntime` is the ONNX Runtime layer used by [TensorStack.OnnxRuntime.Vision](../TensorStack.OnnxRuntime.Vision/README.md) and [TensorStack.OnnxRuntime.LLM](../TensorStack.OnnxRuntime.LLM/README.md).

## Nuget Package
[![OnnxRuntime Badge](https://img.shields.io/nuget/v/TensorStack.OnnxRuntime?color=4bc51e&label=TensorStack.OnnxRuntime)](https://www.nuget.org/packages/TensorStack.OnnxRuntime)

It manages devices, execution providers and model sessions, and handles input type conversion and the creation, use and disposal of `OrtValue` objects.


## Providers
`Provider` uses the DirectML execution provider, and initializes itself on first use.
```csharp
// Best GPU
ExecutionProvider provider = Provider.GetProvider();

// By device type, or device type and id
ExecutionProvider cpuProvider = Provider.GetProvider(DeviceType.CPU);
ExecutionProvider gpuProvider = Provider.GetProvider(DeviceType.GPU, deviceId: 1);

// List devices
foreach (var device in Provider.GetDevices())
{
    Console.WriteLine($"{device.Type}: {device.Name}");
}
```


## Model Sessions
`ModelSession` loads an ONNX model from a `ModelConfig` and runs inference.
```csharp
var config = new ModelConfig
{
    Path = "model.onnx",
    ExecutionProvider = Provider.GetProvider()
};

using (var session = new ModelSession(config))
{
    ModelMetadata metadata = await session.LoadAsync();

    using (var parameters = new ModelParameters(metadata))
    {
        // Inputs are added in model input order
        parameters.AddInput(imageTensor.GetChannels(3), Normalization.ZeroToOne);
        parameters.AddOutput(new[] { 1, 3, height * 4, width * 4 });

        using (var results = await session.RunInferenceAsync(parameters))
        {
            // Read outputs
        }
    }
}
```
