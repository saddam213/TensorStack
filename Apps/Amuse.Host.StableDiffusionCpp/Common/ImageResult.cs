using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public sealed class ImageResult
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("b64_json")]
        public string B64Json { get; set; }
    }

}
