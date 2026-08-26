using TensorStack.Common.Tensor;

namespace TensorStack.StableDiffusionCpp.Common
{
    public sealed class VideoData
    {
        public int Fps { get; set; }
        public AudioTensor Audio { get; set; }
        public ImageTensor[] Frames { get; set; }
    }
}