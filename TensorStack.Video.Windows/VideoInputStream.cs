// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Video;

namespace TensorStack.Video
{
    public class VideoInputStream : VideoInputStreamBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInputStream"/> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <exception cref="System.Exception">Failed to open video file.</exception>
        public VideoInputStream(string filename)
            : this(VideoManager.LoadVideoInfo(filename)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInputStream"/> class.
        /// </summary>
        /// <param name="videoInfo">The video information.</param>
        private VideoInputStream(VideoInfo videoInfo)
            : base(videoInfo) { }


        /// <summary>
        /// Gets the VideoFrame stream.
        /// </summary>
        /// <param name="widthOverride">The width.</param>
        /// <param name="heightOverride">The height.</param>
        /// <param name="frameRateOverride">The frame rate.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>IAsyncEnumerable&lt;ImageFrame&gt;.</returns>
        public IAsyncEnumerable<VideoFrame> GetAsync(int? widthOverride = default, int? heightOverride = default, float? frameRateOverride = default, ResizeMode resizeMode = ResizeMode.Stretch, CancellationToken cancellationToken = default)
        {
            return VideoManager.ReadStreamAsync(SourceFile, frameRateOverride, widthOverride, heightOverride, resizeMode, cancellationToken: cancellationToken);
        }


        /// <summary>
        /// Gets a frame at the specified index position.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="widthOverride">The width override.</param>
        /// <param name="heightOverride">The height override.</param>
        /// <param name="frameRateOverride">The frame rate override.</param>
        /// <param name="resizeMode">The resize mode.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public ValueTask<VideoFrame> GetFrameAsync(int index, int? widthOverride = default, int? heightOverride = default, float? frameRateOverride = default, ResizeMode resizeMode = ResizeMode.Stretch, CancellationToken cancellationToken = default)
        {
            return VideoManager.ReadStreamAsync(SourceFile, frameRateOverride, widthOverride, heightOverride, resizeMode, index, index + 1, cancellationToken: cancellationToken).FirstOrDefaultAsync(CancellationToken.None);
        }


        /// <summary>
        /// Gets a range of frames from the specified start and end indexes.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <param name="widthOverride">The width override.</param>
        /// <param name="heightOverride">The height override.</param>
        /// <param name="frameRateOverride">The frame rate override.</param>
        /// <param name="resizeMode">The resize mode.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public IAsyncEnumerable<VideoFrame> GetFramesAsync(int index, int count, int? widthOverride = default, int? heightOverride = default, float? frameRateOverride = default, ResizeMode resizeMode = ResizeMode.Stretch, CancellationToken cancellationToken = default)
        {
            return VideoManager.ReadStreamAsync(SourceFile, frameRateOverride, widthOverride, heightOverride, resizeMode, index, index + count, cancellationToken: cancellationToken);
        }


        /// <summary>
        /// Move a VideoInputStream asynchronously
        /// </summary>
        /// <param name="videoStream">The video stream.</param>
        /// <param name="newFilename">The new filename.</param>
        /// <returns>A Task&lt;VideoInputStream&gt; representing the asynchronous operation.</returns>
        /// <exception cref="System.Exception">Source video not found</exception>
        /// <exception cref="System.Exception">Destination video already exists</exception>
        public async Task<VideoInputStream> MoveAsync(string newFilename, bool overwrite = true)
        {
            if (!File.Exists(SourceFile))
                throw new Exception("Source video not found");

            File.Move(SourceFile, newFilename, overwrite);
            return await CreateAsync(newFilename);
        }


        /// <summary>
        /// Copy a VideoInputStream asynchronously
        /// </summary>
        /// <param name="videoStream">The video stream.</param>
        /// <param name="newFilename">The new filename.</param>
        /// <returns>A Task&lt;VideoInputStream&gt; representing the asynchronous operation.</returns>
        /// <exception cref="System.Exception">Source video not found</exception>
        /// <exception cref="System.Exception">Destination video already exists</exception>
        public async Task<VideoInputStream> CopyAsync(string newFilename, bool overwrite = true)
        {
            if (!File.Exists(SourceFile))
                throw new Exception("Source video not found");

            File.Copy(SourceFile, newFilename, overwrite);
            return await CreateAsync(newFilename);
        }


        /// <summary>
        /// Create a VideoInputStream asynchronously
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <returns>A Task&lt;VideoInputStream&gt; representing the asynchronous operation.</returns>
        public static async Task<VideoInputStream> CreateAsync(string filename)
        {
            var videoInfo = await VideoManager.LoadVideoInfoAsync(filename);
            return new VideoInputStream(videoInfo);
        }
    }
}
