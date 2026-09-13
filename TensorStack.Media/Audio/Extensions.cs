using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common.Tensor;

namespace TensorStack.Media.Audio
{
    public static class Extensions
    {
        /// <summary>
        /// Saves the Audio to file.
        /// </summary>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="audioFile">The audio file.</param>
        /// <param name="sampleRateOverride">The sample rate override.</param>
        /// <param name="channelsOverride">The channels override.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public static Task SaveAsync(this AudioInputStream audioStream, string audioFile, float? sampleRateOverride = default, int? channelsOverride = default, CancellationToken cancellationToken = default)
        {
            return AudioManager.SaveAudioStreamAsync(audioFile, audioStream, sampleRateOverride, channelsOverride, cancellationToken);
        }


        /// <summary>
        /// Saves the audio to file asynchronously.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="audioTensor">The audio tensor.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static async Task SaveAsync(this AudioTensor audioTensor, string filename,  CancellationToken cancellationToken = default)
        {
            await AudioManager.WriteAudioAsync(filename, audioTensor, cancellationToken);
        }
    }
}
