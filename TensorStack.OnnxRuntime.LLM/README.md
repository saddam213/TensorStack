# TensorStack.OnnxRuntime.LLM
ONNX text generation pipelines for small language, vision and speech models, with greedy and beam search decoding.

## Nuget Package
[![OnnxRuntime LLM Badge](https://img.shields.io/nuget/v/TensorStack.OnnxRuntime.LLM?color=4bc51e&label=TensorStack.OnnxRuntime.LLM)](https://www.nuget.org/packages/TensorStack.OnnxRuntime.LLM)

For GGUF language models with tool calls and image/audio input see [TensorStack.LlamaCpp](../TensorStack.LlamaCpp/README.md).


## Pipelines

| Pipeline | Models | Use |
|---|---|---|
| [LlamaPipeline](Pipelines/Llama/README.md) | Llama 3.2 | Text generation |
| [Phi3Pipeline](Pipelines/Phi/README.md) | Phi-3 | Text generation |
| [WhisperPipeline](Pipelines/Whisper/README.md) | Whisper | Speech recognition and translation |
| [SupertonicPipeline](Pipelines/Supertonic/README.md) | Supertonic | Text to speech |
| FlorencePipeline | Florence-2 | Captioning, OCR, object detection and segmentation |
| SummaryPipeline | T5 style encoder/decoder | Text summarization |


## Decoding
Each text pipeline has two `RunAsync` overloads:
- `GenerateOptions` returns a single `GenerateResult` using greedy or sampled decoding
- `SearchOptions` returns a `GenerateResult[]`, one per beam, using beam search

| Option | Description |
|---|---|
| `MinLength` / `MaxLength` | The generated length in tokens |
| `Beams` | Number of beams for beam search |
| `TopK` / `TopP` / `Temperature` | Sampling |
| `LengthPenalty` | Beam score length penalty |
| `NoRepeatNgramSize` | Blocks repeated n-grams |
| `DiversityLength` | Beam diversity window |
| `EarlyStopping` | `None`, `BestBeam` or `BeamCount` |
| `Seed` | Random seed |


## Florence Example
```csharp
var provider = Provider.GetProvider();
using (var pipeline = FlorencePipeline.Create(provider, "M:\\Models\\Florence-2-base", FlorenceType.Base))
{
    var result = await pipeline.RunAsync(new FlorenceOptions
    {
        TaskType = TaskType.DETAILED_CAPTION,
        Image = new ImageInput("Image.png")
    });
    Console.WriteLine(result.Result);
}
```
Region tasks (`OD`, `DENSE_REGION_CAPTION`, `CAPTION_TO_PHRASE_GROUNDING` etc) also return `CoordinateResults`.
