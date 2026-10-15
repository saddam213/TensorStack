# TensorStack.Python
C# => Python Diffusers Inferface

## Python Pipelines
Once you have created a virtual environment you can now load a pipeline
```csharp
// Pipeline Config
var pipelineConfig = new PipelineConfig
{
    Path = "Qwen/Qwen-Image-Edit",
    Pipeline = "QwenImagePipeline",
    ProcessType = ProcessType.ImageEdit,
    DataType = DataType.Bfloat16,
    MemoryMode = MemoryModeType.OffloadCPU
};

// Create Pipeline Proxy
using (var pythonPipeline = new PythonPipeline(pipelineConfig, PipelineProgress.ConsoleCallback))
{
    // Download/Load Model
    await pythonPipeline.LoadAsync();

    // Generate Option
    var options = new PipelineOptions
    {
        Prompt = "Yarn art style",
        Steps = 30,
        Width = 1024,
        Height = 1024,
        GuidanceScale = 4f,
        Scheduler = SchedulerType.FlowMatchEulerDiscrete,
        ImageInput = new ImageInput("Image.png")
    };

    // Generate
    var response = await pythonPipeline.GenerateAsync(options);

    // Save Image
    await response
        .AsImageTensor()
        .SaveAsync("Result.png");
}
```





## HuggingFace Backends

The `HuggingFaceBackend` class to manage `HuggingFace` backend environments. You can initialize a backend using built-in vendor configurations or provide a custom configuration to control its version, installation directory, and requirements.

### Basic Usage

Create a backend using a built-in vendor configuration:

```csharp
var backend = new HuggingFaceBackend(VendorType.Nvidia);
```

#### Initialize the backend

Use `InitializeAsync()` to initialize the backend environment according to its current state.

```csharp
bool success = await backend.InitializeAsync();
```

Initialization handles the appropriate setup workflow:

- **Not installed:** Download, create, and load the environment.
- **Out of date:** Download, update, and load the environment.
- **Up to date:** Load the existing environment.

The method returns a `bool` indicating whether the operation succeeded.

#### Load an existing backend

Use `LoadAsync()` to load or initialize the backend environment directly.

```csharp
bool success = await backend.LoadAsync();
```

#### Manage the backend environment

You can explicitly control the setup operation by specifying a `BackendStatus` value.

```csharp
bool success = await backend.SetupAsync(BackendStatus.Update);
```

Depending on the supported `BackendStatus` values, setup operations can include creating, updating, rebuilding, or reinstalling the environment.

To delete the backend environment:

```csharp
bool success = await backend.DeleteAsync();
```

To check whether the backend environment exists:

```csharp
bool exists = backend.Exists();
```

### Custom Configuration

You can create a `BackendConfig` to define a custom backend configuration instead of using a built-in vendor default.

```csharp
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

var backend = new HuggingFaceBackend(configuration);
```

The configuration specifies:

| Property | Description |
| --- | --- |
| `Name` | Backend configuration name. |
| `Version` | Python version identifier. |
| `Directory` | Directory used for the backend environment. |
| `Requirements` | List of requirement URLs used by the backend setup process. |

### Using Built-in Configurations

Retrieve a built-in configuration for a supported vendor with `GetConfig()`.

```csharp
BackendConfig configuration = HuggingFaceBackend.GetConfig(VendorType.Nvidia);
```

`BackendConfig` is a C# record, so you can create a modified copy using a `with` expression without modifying the original configuration.

```csharp
var customConfiguration = configuration with
{
    Name = "cuda-test",
    Version = "3.14",
    Directory = "My Location"
};

var backend = new HuggingFaceBackend(customConfiguration);
```

This approach lets you reuse built-in configuration settings while overriding only the properties you need.
