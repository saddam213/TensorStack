using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public sealed class HiresParams
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("upscaler")]
        public string Upscaler { get; set; }

        [JsonPropertyName("scale")]
        public float Scale { get; set; } = 2f;

        [JsonPropertyName("target_width")]
        public int TargetWidth { get; set; }

        [JsonPropertyName("target_height")]
        public int TargetHeight { get; set; }

        [JsonPropertyName("steps")]
        public int Steps { get; set; }

        [JsonPropertyName("denoising_strength")]
        public float DenoisingStrength { get; set; } = 0.7f;

        [JsonPropertyName("upscale_tile_size")]
        public int UpscaleTileSize { get; set; } = 128;

        [JsonPropertyName("custom_sigmas")]
        public float[] CustomSigmas { get; set; }
    }

}