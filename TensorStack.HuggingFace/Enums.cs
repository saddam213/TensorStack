using System.Text.Json.Serialization;

namespace TensorStack.HuggingFace
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

    public enum DataType
    {
        [JsonStringEnumMemberName("float32")]
        Float32 = 0,

        [JsonStringEnumMemberName("bfloat16")]
        Bfloat16 = 1,

        [JsonStringEnumMemberName("float16")]
        Float16 = 2,

        [JsonStringEnumMemberName("float8")]
        Float8 = 3,

        [JsonStringEnumMemberName("int8")]
        Int8 = 6,

        [JsonStringEnumMemberName("int4")]
        Int4 = 7
    }

    public enum MemoryModeType
    {
        /// <summary>
        /// Device Offload
        /// </summary>
        [JsonStringEnumMemberName("OffloadGPU")]
        Device = 0,

        /// <summary>
        /// Sequential CPU Offload
        /// </summary>
        [JsonStringEnumMemberName("OffloadCPU")]
        OffloadCPU = 1,

        /// <summary>
        /// Model CPU Offload
        /// </summary>
        [JsonStringEnumMemberName("OffloadModel")]
        OffloadModel = 2,

        /// <summary>
        /// Hardware Balanced
        /// </summary>
        [JsonStringEnumMemberName("Balanced")]
        Balanced = 3
    }

    public enum ProcessType
    {
        TextToImage = 0,
        ImageToImage = 1,
        ImageEdit = 2,
        ImageInpaint = 3,
        ImageControlNet = 4,
        ImageToImageControlNet = 5,

        TextToVideo = 300,
        ImageToVideo = 301,
        VideoToVideo = 302,

        TextToAudio = 400,
        AudioToText = 500,

        TextToText = 800,
        ImageToText = 801
    }

    public enum QuantizationType
    {
        [JsonStringEnumMemberName("Q16Bit")]
        Q16Bit = 0,

        [JsonStringEnumMemberName("Q8Bit")]
        Q8Bit = 1,

        [JsonStringEnumMemberName("Q4Bit")]
        Q4Bit = 2
    }

    public enum ConversationRole
    {
        [JsonStringEnumMemberName("user")]
        User = 0,

        [JsonStringEnumMemberName("system")]
        System = 1,

        [JsonStringEnumMemberName("assistant")]
        Assistant = 2,

        [JsonStringEnumMemberName("tool")]
        Tool = 3
    }
}
