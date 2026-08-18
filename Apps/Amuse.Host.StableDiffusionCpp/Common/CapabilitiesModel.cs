using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public record CapabilitiesModel
    {
        [JsonPropertyName("samplers")]
        public string[] Samplers { get; set; }


        [JsonPropertyName("schedulers")]
        public string[] Schedulers { get; set; }


        [JsonPropertyName("defaults_by_mode")]
        public DefaultParams DefaultParams { get; set; }
    }

}
