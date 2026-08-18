using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public sealed class JobModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("status")]
        public JobStatus Status { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("started")]
        public long Started { get; set; }

        [JsonPropertyName("completed")]
        public long? Completed { get; set; }

        [JsonPropertyName("queue_position")]
        public int QueuePosition { get; set; }

        [JsonPropertyName("result")]
        public JobResult Result { get; set; }
    }

}
