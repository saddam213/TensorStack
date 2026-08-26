using TensorStack.Common.Tensor;

namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record GenerateImageOptions
    {
        public LoraOptions[] Loras { get; set; }
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int ClipSkip { get; set; }
        public ImageTensor InitImage { get; set; }
        public ImageTensor[] RefImages { get; set; }
        public string RefImageArgs { get; set; }
        public ImageTensor MaskImage { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public SamplerOptions SampleParameters { get; set; }
        public float Strength { get; set; }
        public long Seed { get; set; }
        public int BatchCount { get; set; }
        public ImageTensor ControlImage { get; set; }
        public float ControlStrength { get; set; }
        public ImageTensor IpAdapterImage { get; set; }
        public float IpAdapterStrength { get; set; }
        public PhotoMakerOptions PmParameters { get; set; }
        public PulidOptions PulidParameters { get; set; }
        public TilingOptions VaeTilingParameters { get; set; }
        public CacheOptions Cache { get; set; }
        public HiresOptions Hires { get; set; }
        public int QwenImageLayers { get; set; }
        public bool CircularX { get; set; }
        public bool CircularY { get; set; }
    }
}