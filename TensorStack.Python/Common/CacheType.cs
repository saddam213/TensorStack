using System.Text.Json.Serialization;

namespace TensorStack.Python.Common
{
    public enum CacheType
    {
        [JsonStringEnumMemberName("Dynamic")]
        Dynamic = 0,

        [JsonStringEnumMemberName("DynamicOffload")]
        DynamicOffload = 1,

        [JsonStringEnumMemberName("Static")]
        Static = 2,

        [JsonStringEnumMemberName("StaticOffload")]
        StaticOffload = 3,

        [JsonStringEnumMemberName("Quantized")]
        Quantized = 4,

        [JsonStringEnumMemberName("Disabled")]
        Disabled = 100,
    }
}
