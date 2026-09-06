using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Video;

namespace TensorStack.Media.Video
{
    public static class Extensions
    {
        public static Task SaveAsync(this IAsyncEnumerable<VideoFrame> videoFrames, string videoFile, string videoCodec = "mp4v", int? widthOverride = null, int? heightOverride = null, float? frameRateOverride = null, CancellationToken cancellationToken = default)
        {
            return VideoManager.WriteVideoStreamAsync(videoFile, videoFrames, videoCodec, widthOverride, heightOverride, frameRateOverride, cancellationToken);
        }
    }
}
