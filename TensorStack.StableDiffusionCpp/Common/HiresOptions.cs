namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record HiresOptions
    {
        public bool Enabled { get; set; }
        public HiresUpscaleType Upscaler { get; set; }
        public string ModelPath { get; set; }
        public float Scale { get; set; }
        public int TargetWidth { get; set; }
        public int TargetHeight { get; set; }
        public int Steps { get; set; }
        public float DenoisingStrength { get; set; }
        public int UpscaleTileSize { get; set; }
        public float[] CustomSigmas { get; set; }
    }
}