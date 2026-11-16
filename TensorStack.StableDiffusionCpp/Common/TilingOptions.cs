namespace TensorStack.StableDiffusionCpp.Common
{
    /// <summary>
    /// VAE tiling options, the defaults mirror StableDiffusion.cpp.
    /// </summary>
    public sealed record TilingOptions
    {
        public bool Enabled { get; set; }
        public bool TemporalTiling { get; set; }
        public int TileSizeW { get; set; }
        public int TileSizeH { get; set; }
        public float TargetOverlap { get; set; } = 0.5f;
        public float RelSizeW { get; set; }
        public float RelSizeH { get; set; }
        public string ExtraTilingArgs { get; set; }
    }
}
