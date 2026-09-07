using TensorStack.OnnxRuntime.LLM.Common;

namespace TensorStack.OnnxRuntime.LLM.Pipelines.Llama
{
    public record LlamaConfig : TransformerConfig
    {
        public bool OutputLastHiddenStates { get; set; }
    }
}
