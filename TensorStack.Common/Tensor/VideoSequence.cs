using System;

namespace TensorStack.Common.Tensor
{
    public sealed class VideoSequence
    {
        public VideoSequence(ImageTensor[] frames, float frameRate)
        {
            Frames = frames;
            FrameRate = frameRate;
            Width = Frames[0].Width;
            Height = Frames[0].Height;
            Duration = TimeSpan.FromSeconds(frames.Length / frameRate);
        }

        public VideoSequence(ImageTensor[] frames, float frameRate, AudioTensor audio)
            : this(frames, frameRate)
        {
            Audio = audio;
        }

        public int Width { get; }
        public int Height { get; }
        public float FrameRate { get; }
        public TimeSpan Duration { get; }
        public AudioTensor Audio { get; }
        public ImageTensor[] Frames { get; }
        public bool HasAudio => Audio != null;
        public int SampleRate => Audio?.SampleRate ?? 0;
    }
}
