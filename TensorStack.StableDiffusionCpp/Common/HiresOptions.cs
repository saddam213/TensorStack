namespace TensorStack.StableDiffusionCpp.Common
{
    /// <summary>
    /// Hires options, the defaults mirror StableDiffusion.cpp.
    /// </summary>
    public sealed record HiresOptions
    {
        public bool Enabled { get; set; }
        public HiresUpscaleType Upscaler { get; set; } = HiresUpscaleType.Latent;
        public string ModelPath { get; set; }
        public float Scale { get; set; } = 2f;
        public int TargetWidth { get; set; }
        public int TargetHeight { get; set; }
        public int Steps { get; set; }
        public float DenoisingStrength { get; set; } = 0.7f;
        public int UpscaleTileSize { get; set; } = 128;
        public float[] CustomSigmas { get; set; }
    }
}
