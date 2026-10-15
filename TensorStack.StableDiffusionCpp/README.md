# TensorStack.StableDiffusionCpp
Basic .NET Wrapper API for StableDiffusion.cpp


## Image Example
Generate an image using Krea2 Turbo
```csharp
    using (var pipeline = new StableDiffusionPipeline(configuration))
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
```


## Video Example
Generate an video using MiniMax-H3
```csharp
    using (var pipeline = new StableDiffusionPipeline(configuration))
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
```


## StableDiffusion.cpp Backends

The `StableDiffusionBackend` class to manage `StableDiffusion.cpp` backend environments. You can initialize a backend using built-in vendor configurations or provide a custom configuration to control its version, installation directory, and requirements.

### Basic Usage

Create a backend using a built-in vendor configuration:

```csharp
var backend = new StableDiffusionBackend(VendorType.Nvidia);
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
var configuration = new BackendConfig
{
    Name = "vulkan",
    Version = "956",
    Directory = "StableDiffusionCpp",
    Requirements =
    [
        "https://github.com/leejet/stable-diffusion.cpp/releases/download/master-956-1b0ba10/sd-master-1b0ba10-bin-win-vulkan-x64.zip"
    ]
};

var backend = new StableDiffusionBackend(configuration);
```

The configuration specifies:

| Property | Description |
| --- | --- |
| `Name` | Backend configuration name. |
| `Version` | Backend version identifier. |
| `Directory` | Directory used for the backend environment. |
| `Requirements` | List of requirement URLs used by the backend setup process. |

### Using Built-in Configurations

Retrieve a built-in configuration for a supported vendor with `GetConfig()`.

```csharp
BackendConfig configuration = StableDiffusionBackend.GetConfig(VendorType.Nvidia);
```

`BackendConfig` is a C# record, so you can create a modified copy using a `with` expression without modifying the original configuration.

```csharp
var customConfiguration = configuration with
{
    Name = "vulkan-test",
    Directory = "My Location"
};

var backend = new StableDiffusionBackend(customConfiguration);
```

This approach lets you reuse built-in configuration settings while overriding only the properties you need.
