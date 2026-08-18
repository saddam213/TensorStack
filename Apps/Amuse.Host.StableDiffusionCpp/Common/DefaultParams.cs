using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public record DefaultParams
    {
        [JsonPropertyName("img_gen")]
        public ImageParams ImageParams { get; set; }

        [JsonPropertyName("vid_gen")]
        public VideoParams VideoParams { get; set; }
    }

}
