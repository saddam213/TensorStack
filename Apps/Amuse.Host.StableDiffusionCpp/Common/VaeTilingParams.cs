using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public record VaeTilingParams
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("temporal_tiling")]
        public bool TemporalTiling { get; set; }

        [JsonPropertyName("tile_size_x")]
        public int TileSizeX { get; set; }

        [JsonPropertyName("tile_size_y")]
        public int TileSizeY { get; set; }

        [JsonPropertyName("target_overlap")]
        public float TargetOverlap { get; set; } = 0.5f;

        [JsonPropertyName("rel_size_x")]
        public float RelSizeX { get; set; }

        [JsonPropertyName("rel_size_y")]
        public float RelSizeY { get; set; }

        [JsonPropertyName("extra_tiling_args")]
        public string ExtraTilingArgs { get; set; } = "";
    }
}