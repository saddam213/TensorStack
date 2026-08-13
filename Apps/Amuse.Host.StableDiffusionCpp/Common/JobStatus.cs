using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public enum JobStatus
    {
        [JsonStringEnumMemberName("queued")]
        Queued = 0,

        [JsonStringEnumMemberName("generating")]
        Generating = 1,

        [JsonStringEnumMemberName("completed")]
        Completed = 2,

        [JsonStringEnumMemberName("failed")]
        Failed = 3,

        [JsonStringEnumMemberName("cancelled")]
        Cancelled = 4
    }

}
