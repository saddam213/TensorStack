using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;

namespace TensorStack.Media.Video
{
    public abstract class VideoInputBase : VideoSequence
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInputBase"/> class.
        /// </summary>
        /// <param name="frames">The frames.</param>
        /// <param name="frameRate">The frame rate.</param>
        protected VideoInputBase(ImageTensor[] frames, float frameRate)
            : base(frames, frameRate) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInputBase"/> class.
        /// </summary>
        /// <param name="frames">The frames.</param>
        /// <param name="frameRate">The frame rate.</param>
        /// <param name="audio">The audio.</param>
        protected VideoInputBase(ImageTensor[] frames, float frameRate, AudioTensor audio)
            : base(frames, frameRate, audio) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInputBase"/> class.
        /// </summary>
        /// <param name="videoSequence">The video sequence.</param>
        protected VideoInputBase(VideoSequence videoSequence)
           : base(videoSequence.Frames, videoSequence.FrameRate, videoSequence.Audio) { }


        /// <summary>
        /// Gets the source video filename.
        /// </summary>
        public abstract string SourceFile { get; }

        /// <summary>
        /// Save the Video to file
        /// </summary>
        /// <param name="filename">The filename.</param>
        public abstract void Save(string filename);

        /// <summary>
        /// Save the Video to file
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public abstract Task SaveAsync(string filename, CancellationToken cancellationToken = default);
    }
}
