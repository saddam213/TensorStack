namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record TilingOptions
    {
        public bool Enabled { get; set; }
        public bool TemporalTiling { get; set; }
        public int TileSizeW { get; set; }
        public int TileSizeH { get; set; }
        public float TargetOverlap { get; set; }
        public float RelSizeW { get; set; }
        public float RelSizeH { get; set; }
        public string ExtraTilingArgs { get; set; }
    }
}
