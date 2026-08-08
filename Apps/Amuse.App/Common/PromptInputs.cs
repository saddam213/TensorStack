using System.Collections.Generic;
using TensorStack.Audio;
using TensorStack.Common;
using TensorStack.Image;
using TensorStack.Video;

namespace Amuse.App.Common
{
    public record PromptInputs
    {
        public string Prompt { get; init; }
        public Dictionary<int, string> ImageIndex { get; init; }
        public Dictionary<int, string> AudioIndex { get; init; }
        public Dictionary<int, string> VideoIndex { get; init; }

        public List<TextInput> TextContext { get; init; }
        public List<ImageInput> ImageContext { get; init; }
        public List<AudioInputStream> AudioContext { get; init; }
        public List<VideoInputStream> VideoContext { get; init; }
    }
}
