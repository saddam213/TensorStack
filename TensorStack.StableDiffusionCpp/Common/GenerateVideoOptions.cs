using TensorStack.Common.Tensor;

namespace TensorStack.StableDiffusionCpp.Common
{
    /// <summary>
    /// Video generation options, the defaults mirror StableDiffusion.cpp.
    /// Values left at their default are not sent, so the library default applies.
    /// The high-noise pass shares the base scheduler, sigmas, flow shift and timestep shift.
    /// </summary>
    public sealed record GenerateVideoOptions
    {
        public LoraOptions[] Loras { get; set; }
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int ClipSkip { get; set; } = -1;
        public ImageTensor InitImage { get; set; }
        public ImageTensor EndImage { get; set; }
        public ImageTensor[] RefImages { get; set; }
        public VideoSequence[] RefVideos { get; set; }
        public AudioTensor[] RefAudios { get; set; }
        public ImageTensor[] ControlFrames { get; set; }
        public int Width { get; set; } = 512;
        public int Height { get; set; } = 512;
        public SamplerOptions SampleParameters { get; set; } = new SamplerOptions();
        public SamplerOptions HighNoiseSampleParameters { get; set; } = new SamplerOptions { SampleSteps = -1 };
        public float MoeBoundary { get; set; } = 0.875f;
        public float Strength { get; set; } = 0.75f;
        public long Seed { get; set; } = -1;
        public int VideoFrames { get; set; } = 6;
        public int Fps { get; set; } = 16;
        public float VaceStrength { get; set; } = 1f;
        public TilingOptions VaeTilingParameters { get; set; } = new TilingOptions();
        public CacheOptions Cache { get; set; } = new CacheOptions();
        public HiresOptions Hires { get; set; } = new HiresOptions();
        public bool CircularX { get; set; }
        public bool CircularY { get; set; }
        public string ImagePreprocess { get; set; }
        public string RefImageArgs { get; set; }
        public UpscaleVideoOptions UpscaleOptions { get; set; }
    }
}
