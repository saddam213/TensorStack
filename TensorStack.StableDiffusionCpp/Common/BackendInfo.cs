using System;
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

    public sealed record BackendDevice
    {
        public BackendDevice(string deviceInfo)
        {
            var entries = deviceInfo.Split('\t', 2, StringSplitOptions.TrimEntries);
            Type = entries[0];
            Name = entries[1];
        }

        public string Type { get; init; }
        public string Name { get; init; }
    }
}
