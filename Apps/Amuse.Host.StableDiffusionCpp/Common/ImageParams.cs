using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public record ImageParams
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
        public double Strength { get; set; }

        [JsonPropertyName("batch_count")]
        public int BatchCount { get;} = 1;

        [JsonPropertyName("clip_skip")]
        public int ClipSkip { get; set; } = -1;

        [JsonPropertyName("control_strength")]
        public double ControlStrength { get; set; }

        [JsonPropertyName("embed_image_metadata")]
        public bool EmbedImageMetadata { get; set; }

        [JsonPropertyName("init_image")]
        public string InitImage { get; set; }

        [JsonPropertyName("ref_images")]
        public List<string> RefImages { get; set; } = [];

        [JsonPropertyName("mask_image")]
        public string MaskImage { get; set; }

        [JsonPropertyName("control_image")]
        public string ControlImage { get; set; }

        [JsonPropertyName("sample_params")]
        public SampleParams SampleParams { get; set; }

        [JsonPropertyName("lora")]
        public List<LoraParams> Lora { get; set; } = [];

        [JsonPropertyName("vae_tiling_params")]
        public VaeTilingParams VaeTilingParams { get; set; }

        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; } = "png";

        [JsonPropertyName("output_compression")]
        public int OutputCompression { get; set; } = 100;

        [JsonPropertyName("auto_resize_ref_image")]
        public bool AutoResizeRefImage { get; set; } = true;

        [JsonPropertyName("increase_ref_index")]
        public bool IncreaseRefIndex { get; set; }
    }
}