namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record UpscaleOptions
    {
        public string ModelPath { get; set; }
        public string Backend { get; set; }
        public string ParamsBackend { get; set; }
        public bool IsConvDirect { get; set; } = true;
        public int Threads { get; set; } = -1;
        public int TileSize { get; set; } = 0;
    }
}