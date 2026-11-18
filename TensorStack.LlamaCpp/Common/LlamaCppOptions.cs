namespace TensorStack.LlamaCpp.Common
{
    public sealed record LlamaCppOptions
    {
        public string ModelPath { get; set; }
        public string ProjectorPath { get; set; }
        public PromptTemplate Template { get; set; } = PromptTemplate.Qwen35;
        public int Device { get; set; }
        public string DeviceName { get; set; }
        public int GpuLayers { get; set; } = -1;
        public int ContextSize { get; set; } = 16384;
        public int BatchSize { get; set; } = 2048;
        public int Threads { get; set; }
        public bool? IsFlashAttentionEnabled { get; set; }
        public CacheDataType CacheType { get; set; } = CacheDataType.F16;
        public bool IsCacheOffloadEnabled { get; set; } = true;
        public int ImageMinTokens { get; set; }
        public int ImageMaxTokens { get; set; }
    }
}
