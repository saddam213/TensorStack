namespace TensorStack.LlamaCpp.Common
{
    public sealed record BackendInfo
    {
        public string Version { get; init; }
        public string Commit { get; init; }
        public string SystemInfo { get; init; }
    }
}
