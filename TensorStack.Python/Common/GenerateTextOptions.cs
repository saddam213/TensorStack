using System.Collections.Generic;
using System.Text.Json.Serialization;
using TensorStack.Common.Tensor;

namespace TensorStack.Python.Common
{
    public record GenerateTextOptions
    {
        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("conversation")]
        public ConversationMessage[] Conversation { get; set; }

        [JsonPropertyName("temp_filename")]
        public string TempFileName { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("instruction")]
        public string Instruction { get; set; }

        [JsonPropertyName("task")]
        public string Task { get; set; }

        [JsonPropertyName("min_length")]
        public int MinLength { get; set; }

        [JsonPropertyName("max_length")]
        public int MaxLength { get; set; }

        [JsonPropertyName("do_sample")]
        public bool IsSamplingEnabled { get; set; }

        [JsonPropertyName("num_beams")]
        public int Beams { get; set; }

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("top_k")]
        public float TopK { get; set; }

        [JsonPropertyName("top_p")]
        public float TopP { get; set; }

        [JsonPropertyName("top_h")]
        public float TopH { get; set; }

        [JsonPropertyName("typical_p")]
        public float TypicalP { get; set; }

        [JsonPropertyName("repetition_penalty")]
        public float RepetitionPenalty { get; set; }

        [JsonPropertyName("length_penalty")]
        public float LengthPenalty { get; set; }

        [JsonPropertyName("no_repeat_ngram_size")]
        public int NoRepeatNgramSize { get; set; }

        [JsonPropertyName("enable_thinking")]
        public bool IsThinkingEnabled { get; set; }

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; set; }


        [JsonIgnore]
        public List<ImageTensor> InputImages { get; set; } = [];

        [JsonIgnore]
        public List<AudioTensor> InputAudios { get; set; } = [];
    }
}
