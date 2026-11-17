# TensorStack.AudioCpp
Basic .NET Wrapper API for [audio.cpp](https://github.com/0xShug0/audio.cpp)


## Music Example
Generate music using ACE-Step
```csharp
using (var backend = new AudioCppBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    using (var pipeline = new AudioCppPipeline(backend))
    {
        await pipeline.LoadAsync(new AudioCppOptions
        {
            Pipeline = "ace_step",
            ModelPath = "models/Ace-Step1.5",
            Backend = "cuda"
        });

        AudioTensor audio = await pipeline.GenerateAudioAsync(new GenerateAudioOptions
        {
            Prompt = "cinematic synth pop with clear vocals",
            Prompt2 = "We rise with the morning light",
            Duration = 60,
            Seed = 420
        });
    }
}
```


## Speech Example
Generate speech using Kokoro
```csharp
using (var backend = new AudioCppBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    using (var pipeline = new AudioCppPipeline(backend))
    {
        await pipeline.LoadAsync(new AudioCppOptions
        {
            Pipeline = "kokoro_tts",
            ModelPath = "models/Kokoro-82M-GGUF/kokoro-82m-q8_0.gguf",
            Backend = "cuda"
        });

        AudioTensor audio = await pipeline.GenerateAudioAsync(new GenerateAudioOptions
        {
            Prompt = "Hello from audio.cpp.",
            Language = "en-us",
            Task = "af_heart"
        });
    }
}
```


## Speech Recognition Example
Transcribe speech using Parakeet-TDT
```csharp
using (var backend = new AudioCppBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    using (var pipeline = new AudioCppPipeline(backend))
    {
        await pipeline.LoadAsync(new AudioCppOptions
        {
            Pipeline = "parakeet_tdt",
            ModelPath = "models/Parakeet-TDT-0.6B-v3-GGUF/parakeet-tdt-0.6b-v3-q8_0.gguf",
            Backend = "cuda"
        });

        // AudioInput is part of TensorStack.Media
        AudioTensor inputAudio = await AudioInput.CreateAsync("speech.wav", sampleRate: 16000, channels: 1);

        string text = await pipeline.GenerateTextAsync(new GenerateTextOptions
        {
            InputAudios = [inputAudio]
        });
    }
}
```


## Options
`GenerateAudioOptions` and `GenerateTextOptions` are translated to the request options of the loaded model. Values left at their default are not sent, so the audio.cpp model default applies, and only options the loaded model declares are applied.

| Option | Music | Speech |
|---|---|---|
| `Prompt` | caption or style | text to speak |
| `Prompt2` | lyrics | |
| `Task` | route (`text2music`, `cover`, `full` etc) | built-in voice |
| `InputAudios` | source audio | speaker reference |

Any other audio.cpp option can be passed by name, these map 1:1 onto `--request-option` and `--session-option`:
```csharp
new GenerateAudioOptions
{
    Prompt = "uplifting house music",
    Options = { ["sampler_mode"] = "euler" }
}
```
