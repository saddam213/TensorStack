namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record UpscaleOptions
    {
        public string Backend { get; set; }
        public string ParamsBackend { get; set; }
        public int Threads { get; set; } = -1;
        public int TileSize { get; set; } = 0;
        public string ModelPath { get; set; }
        public string DiffusionModelPath { get; set; }
        public string VaePath { get; set; }
        public int ScaleFactor { get; set; }
        public string FlashAttnMode { get; set; }
        public string ConvDirectMode { get; set; }
    }

    public sealed record UpscaleImageOptions
    {
        public int ScaleFactor { get; set; }
    }

    public sealed record UpscaleVideoOptions
    {
        public long Seed { get; set; }
        public int WindowSize { get; set; } = 16;
        public int TemporalChunkSize { get; set; } = 1;
    }
}