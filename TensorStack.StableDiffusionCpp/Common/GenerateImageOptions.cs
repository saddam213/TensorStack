using TensorStack.Common.Tensor;

namespace TensorStack.StableDiffusionCpp.Common
{
    /// <summary>
    /// Image generation options, the defaults mirror StableDiffusion.cpp.
    /// Values left at their default are not sent, so the library default applies.
    /// </summary>
    public sealed record GenerateImageOptions
    {
        public LoraOptions[] Loras { get; set; }
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int ClipSkip { get; set; } = -1;
        public ImageTensor InitImage { get; set; }
        public ImageTensor[] RefImages { get; set; }
        public string RefImageArgs { get; set; }
        public ImageTensor MaskImage { get; set; }
        public int Width { get; set; } = 512;
        public int Height { get; set; } = 512;
        public SamplerOptions SampleParameters { get; set; } = new SamplerOptions();
        public float Strength { get; set; } = 0.75f;
        public long Seed { get; set; } = -1;
        public int BatchCount { get; set; } = 1;
        public ImageTensor ControlImage { get; set; }
        public float ControlStrength { get; set; } = 0.9f;
        public ImageTensor IpAdapterImage { get; set; }
        public float IpAdapterStrength { get; set; } = 1f;
        public PhotoMakerOptions PmParameters { get; set; } = new PhotoMakerOptions();
        public PulidOptions PulidParameters { get; set; } = new PulidOptions();
        public TilingOptions VaeTilingParameters { get; set; } = new TilingOptions();
        public CacheOptions Cache { get; set; } = new CacheOptions();
        public HiresOptions Hires { get; set; } = new HiresOptions();
        public int QwenImageLayers { get; set; } = 3;
        public bool CircularX { get; set; }
        public bool CircularY { get; set; }
        public string ImagePreprocess { get; set; }
        public ControlNetType ControlNetType { get; set; } = ControlNetType.None;
        public UpscaleImageOptions UpscaleOptions { get; set; }
    }
}
