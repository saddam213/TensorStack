using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public record LoraParams
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("multiplier")]
        public float Multiplier { get; set; } = 1.0f;

        [JsonPropertyName("is_high_noise")]
        public bool IsHighNoise { get; set; }
    }
}