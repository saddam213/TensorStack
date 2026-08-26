namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record LoraOptions
    {
        public bool IsHighNoise { get; set; }
        public float Multiplier { get; set; } = 1;
        public string Path { get; set; }
    }
}