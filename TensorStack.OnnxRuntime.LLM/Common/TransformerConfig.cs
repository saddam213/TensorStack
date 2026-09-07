using TensorStack.OnnxRuntime.LLM.Tokenizers;

namespace TensorStack.OnnxRuntime.LLM.Common
{
    public abstract record TransformerConfig
    {
        public ITokenizer Tokenizer { get; set; }
        public EncoderConfig EncoderConfig { get; set; }
        public DecoderConfig DecoderConfig { get; set; }
    }
}
