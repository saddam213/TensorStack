using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Amuse.Host.StableDiffusionCpp.Common
{
    public record SampleParams
    {
        [JsonPropertyName("scheduler")]
        public string Scheduler { get; set; }

        [JsonPropertyName("sample_method")]
        public string SampleMethod { get; set; }

        [JsonPropertyName("sample_steps")]
        public int SampleSteps { get; set; }

        [JsonPropertyName("eta")]
        public float? Eta { get; set; }

        [JsonPropertyName("shifted_timestep")]
        public int ShiftedTimestep { get; set; }

        [JsonPropertyName("custom_sigmas")]
        public List<float> CustomSigmas { get; set; } = [];

        [JsonPropertyName("flow_shift")]
        public float? FlowShift { get; set; }

        [JsonPropertyName("guidance")]
        public GuidanceParams Guidance { get; set; }
    }
}