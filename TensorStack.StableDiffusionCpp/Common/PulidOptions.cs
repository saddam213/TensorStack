namespace TensorStack.StableDiffusionCpp.Common
{
    /// <summary>
    /// PuLID options, the defaults mirror StableDiffusion.cpp.
    /// </summary>
    public sealed record PulidOptions
    {
        public string IdEmbeddingPath { get; set; }
        public float IdWeight { get; set; } = 1f;
    }
}
