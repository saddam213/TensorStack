using TensorStack.Common;

namespace TensorStack.LlamaCpp.Common
{
    public sealed record BackendDevice
    {
        public DeviceType Type { get; init; }
        public string Name { get; init; }
        public string Backend { get; init; }
        public bool IsIntegrated { get; init; }
        public string Description { get; init; }
    }
}
