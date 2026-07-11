using System.Collections.Generic;
using System.Text.Json.Serialization;
using TensorStack.Common.Tensor;
using TensorStack.Python.Scheduler;

namespace TensorStack.Python.Common
{
    public record GenerateAudioOptions
    {
        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("prompt2")]
        public string Prompt2 { get; set; }

        [JsonPropertyName("negative_prompt")]
        public string NegativePrompt { get; set; }

        [JsonPropertyName("guidance_scale")]
        public float GuidanceScale { get; set; } = 1;

        [JsonPropertyName("guidance_scale2")]
        public float GuidanceScale2 { get; set; } = 1;

        [JsonPropertyName("steps")]
        public int Steps { get; set; } = 50;

        [JsonPropertyName("steps2")]
        public int Steps2 { get; set; } = 20;

        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 1;

        [JsonPropertyName("temp_filename")]
        public string TempFileName { get; set; }

        [JsonPropertyName("enable_vae_tiling")]
        public bool EnableVaeTiling { get; set; }

        [JsonPropertyName("enable_vae_slicing")]
        public bool EnableVaeSlicing { get; set; }

        [JsonPropertyName("duration")]
        public float Duration { get; set; } = 5f;

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("instruction")]
        public string Instruction { get; set; }

        [JsonPropertyName("task")]
        public string Task { get; set; }

        [JsonPropertyName("max_length")]
        public int MaxLength { get; set; }

        [JsonPropertyName("max_length2")]
        public int MaxLength2 { get; set; }

        [JsonPropertyName("bpm")]
        public int Bpm { get; set; }

        [JsonPropertyName("keyscale")]
        public string Keyscale { get; set; }

        [JsonPropertyName("track_name")]
        public string TrackName { get; set; }

        [JsonPropertyName("time_signature")]
        public string TimeSignature { get; set; }

        [JsonPropertyName("speed")]
        public float Speed { get; set; }

        [JsonPropertyName("silence_duration")]
        public float SilenceDuration { get; set; }

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; set; }

        [JsonPropertyName("scheduler_options")]
        public SchedulerOptions SchedulerOptions { get; set; }

        [JsonPropertyName("lora_options")]
        public List<LoraOptions> LoraOptions { get; set; }

        [JsonIgnore]
        public List<AudioTensor> InputAudios { get; set; } = [];
    }
}
