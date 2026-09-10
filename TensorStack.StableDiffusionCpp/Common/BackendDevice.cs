using System;
using TensorStack.Common;

namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record BackendDevice
    {
        public BackendDevice(string deviceInfo)
        {
            var entries = deviceInfo.Split('\t', 2, StringSplitOptions.TrimEntries);
            if (entries.Length < 2)
                throw new Exception("Invalid deviceInfo");

            Name = entries[1];
            Backend = entries[0].ToLower();
            Type = Backend.Equals("cpu") ? DeviceType.CPU : DeviceType.GPU;
        }

        public DeviceType Type { get; init; }
        public string Name { get; init; }
        public string Backend { get; set; }
    }
}
