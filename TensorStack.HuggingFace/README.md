# TensorStack.HuggingFace
Basic .NET Wrapper API for HuggingFace Diffusers & Transformers

## Python Pipelines
Generate an image using Qwen-Image-Edit
```csharp
using (var backend = new HuggingFaceBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

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
}
```


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
// Requirements  - List of python package requiremnets.

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
