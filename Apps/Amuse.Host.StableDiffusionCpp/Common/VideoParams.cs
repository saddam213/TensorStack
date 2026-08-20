using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public record VideoParams
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("negative_prompt")]
        public string NegativePrompt { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }

        [JsonPropertyName("strength")]
        public float Strength { get; set; } = 1f;

        [JsonPropertyName("clip_skip")]
        public int ClipSkip { get; set; } = -1;

        [JsonPropertyName("video_frames")]
        public int Frames { get; set; } = 33;

        [JsonPropertyName("fps")]
        public int FrameRate { get; set; } = 16;

        [JsonPropertyName("moe_boundary")]
        public float MoeBoundary { get; set; } = 0.875f;

        [JsonPropertyName("vace_strength")]
        public float VaceStrength { get; set; } = 1f;

        [JsonPropertyName("init_image")]
        public string ImageFirst { get; set; }

        [JsonPropertyName("end_image")]
        public string ImageLast { get; set; }

        [JsonPropertyName("control_frames")]
        public List<string> ControlFrames { get; set; }

        [JsonPropertyName("sample_params")]
        public SampleParams SampleParams { get; set; }

        [JsonPropertyName("high_noise_sample_params")]
        public SampleParams SampleParamsHighNoise { get; set; }

        [JsonPropertyName("lora")]
        public List<LoraParams> Lora { get; set; } = [];

        [JsonPropertyName("vae_tiling_params")]
        public VaeTilingParams VaeTilingParams { get; set; }

        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; } = "webm";

        [JsonPropertyName("output_compression")]
        public int OutputCompression { get; set; } = 100;

        [JsonPropertyName("auto_resize_ref_image")]
        public bool AutoResizeRefImage { get; set; } = true;

        [JsonPropertyName("increase_ref_index")]
        public bool IncreaseRefIndex { get; set; }

        [JsonPropertyName("hires")]
        public HiresParams HiresParams { get; set; }
    }
}