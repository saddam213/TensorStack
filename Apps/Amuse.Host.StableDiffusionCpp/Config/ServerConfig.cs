using Amuse.Common;
using Amuse.Host.StableDiffusionCpp.Common;
using System.Collections.Generic;

namespace Amuse.Host.StableDiffusionCpp.Config
{
    public record ServerConfig
    {
        public bool IsDebug { get; set; }
        public string Directory { get; set; }
        public short Port { get; set; }
        public string Address { get; set; }
        public string BaseUrl => $"http://{Address}:{Port}/";
        public Dictionary<string, string> ServerVariables { get; set; }
        public ModelConfig ModelConfig { get; set; }

        // Device/Memory
        public int DeviceId { get; set; }
        public BackendType Backend { get; set; }
        public int MemoryReserve { get; set; } = 1;
        public MemoryModeType MemoryMode { get; set; }
        public QuantizationType QuantizationType { get; set; }
        public bool IsFlashAttentionEnabled { get; set; }
    }
}
