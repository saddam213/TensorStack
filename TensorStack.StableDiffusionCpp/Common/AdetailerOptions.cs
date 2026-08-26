namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record AdetailerOptions
    {
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public string ExtraAdArgs { get; set; }
    }
}