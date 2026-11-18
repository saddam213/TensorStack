# TensorStack.HuggingFace
Basic .NET Wrapper API for HuggingFace Diffusers & Transformers

## Nuget Package
[![HuggingFace Badge](https://img.shields.io/nuget/v/TensorStack.HuggingFace?color=4bc51e&label=TensorStack.HuggingFace)](https://www.nuget.org/packages/TensorStack.HuggingFace)

## Image Example
Edit an image using Qwen-Image-Edit
```csharp
using (var backend = new HuggingFaceBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    // Pipeline Config
    var pipelineConfig = new PipelineConfig
    {
        Pipeline = "QwenImagePipeline",
        Template = "QwenImage_Edit",
        ProcessType = ProcessType.ImageEdit,
        DataType = DataType.Bfloat16,
        MemoryMode = MemoryModeType.OffloadCPU,
        CheckpointConfig = new CheckpointConfig
        {
            Transformer = "models/Qwen-Image-Edit-2511/transformer",
            TextEncoder = "models/Qwen2.5-VL-7B/text_encoder",
            Vae = "models/Qwen-Image/vae"
        }
    };

    using (var pipeline = new HuggingFacePipeline(pipelineConfig, PipelineProgress.ConsoleCallback))
    {
        // Load Model
        await pipeline.LoadAsync();

        // Generate
        var results = await pipeline.GenerateImageAsync(new GenerateImageOptions
        {
            Prompt = "Yarn art style",
            Steps = 30,
            Width = 1024,
            Height = 1024,
            GuidanceScale = 4f,
            SchedulerOptions = new FlowMatchEulerOptions(),
            InputImages = [new ImageInput("Image.png")]
        });

        // Save Image
        await results[0].SaveAsync("Result.png");
    }
}
```


## Pipelines
`Pipeline` names a Python pipeline module in `Native/Pipelines` (e.g. `FluxPipeline`, `WanPipeline`, `LTX20Pipeline`, `AceStepPipeline`, `AutoTextPipeline`), and `Template` names a folder in `Native/Pipelines/Templates` holding the model's config files. `CheckpointConfig` gives the path of each model component.

| Method | Returns | Process Types |
|---|---|---|
| `GenerateImageAsync` | `ImageTensor[]` | `TextToImage`, `ImageToImage`, `ImageEdit`, `ImageInpaint`, `ImageControlNet` |
| `GenerateVideoAsync` | `Tensor<float>[]` | `TextToVideo`, `ImageToVideo`, `VideoToVideo` |
| `GenerateAudioAsync` | `AudioTensor[]` | `TextToAudio` |
| `GenerateTextAsync` | `TextInput[]` | `TextToText`, `ImageToText` |

`ReloadAsync` changes the process type, LoRA adapters or ControlNet without reloading the model.


## HuggingFace Backends

The `HuggingFaceBackend` class to manage `HuggingFace` backend environments. You can initialize a backend using built-in vendor configurations or provide a custom configuration to control its version, installation directory, and requirements.

#### Basic Documentation
```csharp
// Create a backend using built-in vendor defaults.
var backend = new HuggingFaceBackend(VendorType.Nvidia);

// Initialize the environment.
// Attempts to load an existing environment, update it if loading
// fails, or create a new environment if necessary.
bool initialized = await backend.InitializeAsync();

// Load the environment directly.
bool loaded = await backend.LoadAsync();

// Perform a specific setup operation.
bool updated = await backend.SetupAsync(BackendStatus.Update);

// Delete the backend environment.
bool deleted = await backend.DeleteAsync();

// Check whether the backend environment exists.
bool exists = backend.Exists();


// ============================================================
// Backend Setup Operations
// ============================================================
// Create   - Create a new environment.
// Load     - Load operation status.
// Update   - Update an existing environment.
// Rebuild  - Rebuild the environment.
// Reinstall - Reinstall the environment.

bool success = await backend.SetupAsync(BackendStatus.Create);


// ============================================================
// Custom Configuration
// ============================================================
// Name          - Backend name
// Version       - Python version (Supported: 3.12, 3.13, 3.14)
// Directory     - Backend directory
// Requirements  - List of python package requirements.

// Define a custom backend configuration.
var customConfiguration = new BackendConfig
{
    Name = "cuda",
    Version = "3.12",
    Directory = "HuggingFace",
    Requirements =
    [
        "your custom python requirements",

        // Include Default Requirements
        .. HuggingFaceBackend.DefaultRequirements
    ]
};

// Create a backend using the custom configuration.
var customBackend = new HuggingFaceBackend(customConfiguration);


// ============================================================
// Built-in Configuration Customization
// ============================================================

// Retrieve a built-in vendor configuration.
BackendConfig configuration = HuggingFaceBackend.GetConfig(VendorType.Nvidia);

// Create a modified copy of the configuration.
// BackendConfig is a record, so the original remains unchanged.
var modifiedConfiguration = configuration with
{
    Name = "cuda-test",
    Version = "3.14",
    Directory = "My Location"
};

// Create a backend using the modified configuration.
var modifiedBackend = new HuggingFaceBackend(modifiedConfiguration);
```
