using TensorStack.Common.Tensor;

namespace TensorStack.StableDiffusionCpp.Common
{
    /// <summary>
    /// PhotoMaker options, the defaults mirror StableDiffusion.cpp.
    /// </summary>
    public sealed record PhotoMakerOptions
    {
        public ImageTensor[] IdImages { get; set; }
        public string IdEmbedPath { get; set; }
        public float StyleStrength { get; set; } = 20f;
    }
}
