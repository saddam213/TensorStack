namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record TilingOptions
    {
        public bool Enabled { get; set; }
        public bool TemporalTiling { get; set; }
        public int TileSizeX { get; set; }
        public int TileSizeY { get; set; }
        public float TargetOverlap { get; set; }
        public float RelSizeX { get; set; }
        public float RelSizeY { get; set; }
        public string ExtraTilingArgs { get; set; }
    }
}
