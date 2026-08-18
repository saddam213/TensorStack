using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public sealed class JobResult
    {
        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }

        [JsonPropertyName("fps")]
        public int FrameRate { get; set; }

        [JsonPropertyName("frame_count")]
        public int FrameCount { get; set; }

        [JsonPropertyName("images")]
        public List<ImageResult> Images { get; set; } = [];

        [JsonPropertyName("b64_json")]
        public string Video { get; set; }

        public byte[] GetImageBytes(int index = 0)
        {
            var image = Images.ElementAtOrDefault(index);
            if (image == null)
                return null;

            return Convert.FromBase64String(image.B64Json);
        }


        public byte[] GetVideoBytes()
        {
            if (string.IsNullOrEmpty(Video))
                return null;

            return Convert.FromBase64String(Video);
        }
    }

}
