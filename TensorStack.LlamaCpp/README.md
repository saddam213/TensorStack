# TensorStack.LlamaCpp
Basic .NET Wrapper API for [llama.cpp](https://github.com/ggml-org/llama.cpp)

## Nuget Package
[![LlamaCpp Badge](https://img.shields.io/nuget/v/TensorStack.LlamaCpp?color=4bc51e&label=TensorStack.LlamaCpp)](https://www.nuget.org/packages/TensorStack.LlamaCpp)

Runs GGUF language models (Qwen 3.x, Gemma 4) with streaming, thinking, tool calls and image/audio input.
Thinking and tool calls are returned in the same normalized form for every model: `<think>...</think>` and `<tool_call>...</tool_call>`.


## Chat Example
Generate text using Qwen 3.5, tokens are reported to the progress callback as they are generated
```csharp
using (var backend = new LlamaCppBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    var progress = new Progress<PipelineProgress>(x => Console.Write(x.Message));
    using (var pipeline = new LlamaCppPipeline(backend, progress))
    {
        await pipeline.LoadAsync(new LlamaCppOptions
        {
            ModelPath = "models/Qwen3.5-9B-GGUF/Qwen3.5-9B-Q8_0.gguf",
            Template = PromptTemplate.Qwen35
        });

        TextInput[] result = await pipeline.GenerateTextAsync(new GenerateTextOptions
        {
            IsThinkingEnabled = true,
            Conversation =
            [
                new ConversationMessage(ConversationRole.System, "You are a helpful assistant."),
                new ConversationMessage(ConversationRole.User, "What is the capital of France?")
            ]
        });
    }
}
```


## Tool Call Example
Tools are OpenAI style function schemas. When the model calls a tool the result contains a `<tool_call>` block, add the call and its result to the conversation and generate again
```csharp
using (var backend = new LlamaCppBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    using (var pipeline = new LlamaCppPipeline(backend))
    {
        await pipeline.LoadAsync(new LlamaCppOptions
        {
            ModelPath = "models/Qwen3.5-9B-GGUF/Qwen3.5-9B-Q8_0.gguf",
            Template = PromptTemplate.Qwen35
        });

        var options = new GenerateTextOptions
        {
            Tools = [File.ReadAllText("calculator.json")],
            Conversation = [new ConversationMessage(ConversationRole.User, "Multiply 12.5 by 34")]
        };
        TextInput[] result = await pipeline.GenerateTextAsync(options);

        // The model asked for the tool, run it and return the result
        options.Conversation.Add(new ConversationMessage(ConversationRole.Assistant, "", ToolCalls: ["""{"name":"calculator","arguments":{"op":"multiply","a":12.5,"b":34}}"""]));
        options.Conversation.Add(new ConversationMessage(ConversationRole.Tool, "425"));
        result = await pipeline.GenerateTextAsync(options);
    }
}
```


## Vision Example
Describe an image using Gemma 4, image and audio input needs the multimodal projector (mmproj) of the model
```csharp
using (var backend = new LlamaCppBackend(VendorType.Nvidia))
{
    await backend.InitializeAsync();

    using (var pipeline = new LlamaCppPipeline(backend))
    {
        await pipeline.LoadAsync(new LlamaCppOptions
        {
            ModelPath = "models/Gemma-4-E4B-IT-GGUF/gemma-4-E4B-it-Q8_0.gguf",
            ProjectorPath = "models/Gemma-4-E4B-IT-GGUF/mmproj-gemma-4-E4B-it-Q8_0.gguf",
            Template = PromptTemplate.Gemma4
        });

        TextInput[] result = await pipeline.GenerateTextAsync(new GenerateTextOptions
        {
            InputImages = [image],
            Conversation = [new ConversationMessage(ConversationRole.User, "Describe this image.", ImageIndex: [0])]
        });
    }
}
```


## Options
`LlamaCppOptions` are set when the model is loaded.

| Option | Description |
|---|---|
| `Template` | The prompt format of the model family: `Qwen30`, `Qwen35`, `Qwen36`, `Qwen38`, `Gemma4` |
| `ProjectorPath` | The multimodal projector (mmproj GGUF), required for image and audio input |
| `Device` / `DeviceName` | The GPU to use, by index or by name |
| `GpuLayers` | Layers to offload to the GPU, `-1` for all, `0` for CPU only |
| `ContextSize` | The context size in tokens, the conversation and the generated text must fit in it, `GenerateTextOptions.ContextSize` changes it per request without reloading the model |
| `CacheType` | The key/value cache storage type, `Q8_0` and `Q4_0` use less memory |

The conversation is cached between calls, so a follow up message only evaluates the new part of the conversation.
Models with recurrent state (Qwen 3.5 and later) can not rewind their cache and evaluate the whole conversation each call.
