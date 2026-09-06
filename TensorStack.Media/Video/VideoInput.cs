using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;
using TensorStack.Media.Video;

namespace TensorStack.Media.Windows.Video
{
    public sealed class VideoInput : VideoInputBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInput"/> class.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        public VideoInput(string sourceFile)
            : base(VideoManager.LoadVideoSequence(sourceFile))
        {
            SourceFile = sourceFile;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInput"/> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="videoSequence">The video sequence.</param>
        public VideoInput(string filename, VideoSequence videoSequence)
            : base(videoSequence)
        {
            SourceFile = filename;
        }


        /// <summary>
        /// Gets the source video filename.
        /// </summary>
        public override string SourceFile { get; }


        /// <summary>
        /// Save the Video to file
        /// </summary>
        /// <param name="filename">The filename.</param>
        public override void Save(string filename)
        {
            SaveAsync(filename).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Save the Video to file
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public override async Task SaveAsync(string filename, CancellationToken cancellationToken = default)
        {
            await MediaManager.SaveAsync(this, filename, cancellationToken).ConfigureAwait(false);
        }
    }
}
