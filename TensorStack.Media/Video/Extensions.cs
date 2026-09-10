using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;
using TensorStack.Common.Video;

namespace TensorStack.Media.Video
{
    public static class Extensions
    {
        public static Task SaveAsync(this IAsyncEnumerable<VideoFrame> videoFrames, string videoFile, string videoCodec = "mp4v", int? widthOverride = null, int? heightOverride = null, float? frameRateOverride = null, CancellationToken cancellationToken = default)
        {
            return VideoManager.WriteVideoStreamAsync(videoFile, videoFrames, videoCodec, widthOverride, heightOverride, frameRateOverride, cancellationToken);
        }


        public static VideoSequence GetSequence(this VideoSequence videoSequence, TimeSpan duration)
        {
            return videoSequence.GetSequence(TimeSpan.Zero, duration);
        }


        public static VideoSequence GetSequence(this VideoSequence videoSequence, TimeSpan start, TimeSpan duration)
        {
            var startFrame = (int)Math.Round(start.TotalSeconds * videoSequence.FrameRate);
            var frameCount = (int)Math.Round(duration.TotalSeconds * videoSequence.FrameRate);
            return videoSequence.GetSequence(startFrame, frameCount);
        }


        public static VideoSequence GetSequence(this VideoSequence videoSequence, int frameCount)
        {
            return videoSequence.GetSequence(0, frameCount);
        }


        public static VideoSequence GetSequence(this VideoSequence videoSequence, int startFrame, int frameCount)
        {
            var frameRate = videoSequence.FrameRate;
            if (startFrame + frameCount > videoSequence.FrameCount)
                throw new ArgumentOutOfRangeException(nameof(frameCount));

            var frames = videoSequence.Frames[startFrame..(startFrame + frameCount)];
            var audio = videoSequence.Audio?.GetSequence(startFrame / frameRate, frameCount / frameRate);
            return new VideoSequence(frames, frameRate, audio);
        }

    }
}
