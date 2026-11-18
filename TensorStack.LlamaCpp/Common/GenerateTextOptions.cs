using System.Collections.Generic;
using TensorStack.Common.Tensor;

namespace TensorStack.LlamaCpp.Common
{
    public sealed record GenerateTextOptions
    {
        public int Seed { get; set; } = -1;
        public string Prompt { get; set; }
        public List<ConversationMessage> Conversation { get; set; }
        public int MaxLength { get; set; } = 4096;
        public int ContextSize { get; set; }
        public bool IsSamplingEnabled { get; set; } = true;
        public float Temperature { get; set; } = 0.7f;
        public int TopK { get; set; } = 20;
        public float TopP { get; set; } = 0.95f;
        public float MinP { get; set; }
        public float TypicalP { get; set; } = 1f;
        public float RepetitionPenalty { get; set; } = 1f;
        public int RepetitionPenaltyRange { get; set; } = 64;
        public bool IsThinkingEnabled { get; set; }
        public string[] Tools { get; set; }

        public List<ImageTensor> InputImages { get; set; } = [];
        public List<AudioTensor> InputAudios { get; set; } = [];
    }
}
