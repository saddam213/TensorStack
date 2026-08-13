using System.Text.Json.Serialization;
namespace Amuse.Host.StableDiffusionCpp.Common
{
    public record GuidanceParams
    {
        [JsonPropertyName("txt_cfg")]
        public float? TxtCfg { get; set; }

        [JsonPropertyName("img_cfg")]
        public float? ImgCfg { get; set; }

        [JsonPropertyName("distilled_guidance")]
        public float? DistilledGuidance { get; set; }
    }
}