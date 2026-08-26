using TensorStack.Common.Tensor;

namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record GenerateVideoOptions
    {
        public LoraOptions[] Loras { get; set; }
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int ClipSkip { get; set; }
        public ImageTensor InitImage { get; set; }
        public ImageTensor EndImage { get; set; }
        public ImageTensor[] RefImages { get; set; }
        public VideoData[] RefVideos { get; set; }
        public AudioTensor[] RefAudios { get; set; }
        public ImageTensor[] ControlFrames { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public SamplerOptions SampleParameters { get; set; }
        public SamplerOptions HighNoiseSampleParameters { get; set; }
        public float MoeBoundary { get; set; }
        public float Strength { get; set; }
        public long Seed { get; set; }
        public int VideoFrames { get; set; }
        public int Fps { get; set; }
        public float VaceStrength { get; set; }
        public TilingOptions VaeTilingParameters { get; set; }
        public CacheOptions Cache { get; set; }
        public HiresOptions Hires { get; set; }
        public bool CircularX { get; set; }
        public bool CircularY { get; set; }
    }
}