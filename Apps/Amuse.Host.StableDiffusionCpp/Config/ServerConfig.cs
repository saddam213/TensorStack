using Amuse.Common;
using Amuse.Host.StableDiffusionCpp.Common;
using System.Collections.Generic;

namespace Amuse.Host.StableDiffusionCpp.Config
{
    public record ServerConfig
    {
        public short Port { get; set; } = 1234;
        public string Address { get; set; } = "127.0.0.1";
        public string BaseUrl => $"http://{Address}:{Port}/";
        public Dictionary<string, string> ServerVariables { get; set; }
        public ModelConfig ModelConfig { get; set; }

        // Device/Memory
        public int DeviceId { get; set; }
        public BackendType Backend { get; set; }
        public int MemoryReserve { get; set; } = 1;
        public MemoryModeType MemoryMode { get; set; }
        public bool IsFlashAttentionEnabled { get; set; }
    }
}
