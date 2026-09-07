using TensorStack.OnnxRuntime.LLM.Common;

namespace TensorStack.OnnxRuntime.LLM.Pipelines.Whisper
{
    public record WhisperConfig : TransformerConfig
    {
        public string MelFiltersPath { get; init; }
    }
}
