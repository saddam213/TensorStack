using System.Collections.Generic;

namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record BackendInfo
    {
        public string Version { get; init; }
        public string Commit { get; init; }
        public string SystemInfo { get; init; }
        public IReadOnlyList<BackendDevice> Devices { get; init; }
        public int NumPhysicalCores { get; set; }
    }
}
