# TensorStack.StableDiffusionCpp
Basic .NET Wrapper API for StableDiffusion.cpp


## Image Example
Generate an image using Krea2 Turbo
```csharp
using (var backend = new StableDiffusionBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    using (var pipeline = new StableDiffusionPipeline(backend))
    {
        var device = pipeline.Devices.FirstOrDefault(x => x.Type == DeviceType.GPU);
        await pipeline.LoadContextAsync(new ContextOptions
        {
            Backend = device.Backend,
            ParamsBackend = "*=cpu",
            MaxVram = "-1",
            FlashAttn = true,
            DiffusionFlashAttn = true,

            LlmPath = "Qwen3VL-4B-Instruct-Q8_0.gguf",
            VaePath = "diffusion_pytorch_model.safetensors",
            DiffusionModelPath = "Krea-2-Turbo-Q8_0.gguf",
        });

        var defaultOptions = pipeline.DefaultImageOptions;
        var options = defaultOptions with
        {
            Prompt = "cute cat",
            Width = 1024,
            Height = 1024,
            Seed = 420,
            SampleParameters = defaultOptions.SampleParameters with
            {
                TxtCfg = 1,
                SampleSteps = 8,
                SampleMethod = SamplerType.Euler,
            }
        };

        var result = await pipeline.GenerateImageAsync(options);
        var imageTensor = result.FirstOrDefault();
        await imageTensor.SaveAsync("OutputImage.png");
    }
}
```


## Video Example
Generate an video using MiniMax-H3
```csharp
using (var backend = new StableDiffusionBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    using (var pipeline = new StableDiffusionPipeline(backend))
    {
        var device = pipeline.Devices.FirstOrDefault(x => x.Type == DeviceType.GPU);
        await pipeline.LoadContextAsync(new ContextOptions
        {
            Backend = device.Backend,
            ParamsBackend = "*=cpu",
            MaxVram = "-1",
            FlashAttn = true,
            DiffusionFlashAttn = true,

            LlmPath = "qwen3vl_32b_minimax_h3-Q4_K_M.gguf",
            VaePath = "minimax_h3_video_vae_fp16.safetensors",
            AudioVaePath = "minimax_h3_audio_vae_fp32.safetensors",
            DiffusionModelPath = "minimax_h3_fl2va_pruned-Q8_0.gguf",
        });

        var defaultOptions = pipeline.DefaultVideoOptions;
        var options = defaultOptions with
        {
            Prompt = "cute cat",
            Width = 864,
            Height = 480,
            Fps = 24,
            Seed = 420,
            VideoFrames = 56,
            SampleParameters = defaultOptions.SampleParameters with
            {
                TxtCfg = 1,
                SampleSteps = 20,
                SampleMethod = SamplerType.Euler,
            }
        };

        var videoSequence = await pipeline.GenerateVideoAsync(options);
        await videoSequence.SaveAsync("OutputVideo.mp4");
    }
}
```


## StableDiffusion.cpp Backends

The `StableDiffusionBackend` class to manage `StableDiffusion.cpp` backend environments. You can initialize a backend using built-in vendor configurations or provide a custom configuration to control its version, installation directory, and requirements.

#### Basic Documentation
```csharp
// Create a backend using built-in vendor defaults.
var backend = new StableDiffusionBackend(VendorType.Nvidia);

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
// Version       - StableDiffusionCpp version
// Directory     - Backend directory
// Requirements  - List of downloadrequiremnets.

// Define a custom backend configuration.
var customConfiguration = new BackendConfig
{
    Name = "vulkan",
    Version = "956",
    Directory = "StableDiffusionCpp",
    Requirements =
    [
        "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/sd-master-1b0ba10-bin-win-vulkan-x64.zip"
    ]
};

// Create a backend using the custom configuration.
var customBackend = new StableDiffusionBackend(customConfiguration);


// ============================================================
// Built-in Configuration Customization
// ============================================================

// Retrieve a built-in vendor configuration.
BackendConfig configuration = StableDiffusionBackend.GetConfig(VendorType.Nvidia);

// Create a modified copy of the configuration.
// BackendConfig is a record, so the original remains unchanged.
var modifiedConfiguration = configuration with
{
    Name = "vulkan-test",
    Directory = "My Location"
};

// Create a backend using the modified configuration.
var modifiedBackend = new StableDiffusionBackend(modifiedConfiguration);
```