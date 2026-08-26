using TensorStack.Common.Tensor;

namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed record PhotoMakerOptions
    {
        public ImageTensor[] IdImages { get; set; }
        public string IdEmbedPath { get; set; }
        public float StyleStrength { get; set; }
    }
}