using System;
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
        public static async Task SaveAsync(this AudioTensor audioTensor, string filename, CancellationToken cancellationToken = default)
        {
            await AudioManager.WriteAudioAsync(filename, audioTensor, cancellationToken);
        }


        /// <summary>
        /// Resamples the audio to the sample rate, every channel is resampled.
        /// </summary>
        /// <param name="audioTensor">The audio tensor.</param>
        /// <param name="sampleRate">The target sample rate.</param>
        /// <returns>The resampled audio, the same instance when the sample rate already matches.</returns>
        public static AudioTensor Resample(this AudioTensor audioTensor, int sampleRate)
        {
            if (audioTensor.SampleRate == sampleRate)
                return audioTensor;

            // Channels
            var channels = audioTensor.Channels;
            var sourceCount = audioTensor.Samples;
            var sampleCount = AudioResampler.GetSampleCount(sourceCount, audioTensor.SampleRate, sampleRate);
            var samples = new float[channels * sampleCount];
            for (int channel = 0; channel < channels; channel++)
            {
                var channelSamples = AudioResampler.Resample(audioTensor.Memory.Span.Slice(channel * sourceCount, sourceCount), audioTensor.SampleRate, sampleRate);
                channelSamples.CopyTo(samples.AsSpan(channel * sampleCount, sampleCount));
            }

            // Result
            return new AudioTensor(new Tensor<float>(samples, [channels, sampleCount]), sampleRate);
        }


        /// <summary>
        /// Mixes the audio channels down to mono.
        /// </summary>
        /// <param name="audioTensor">The audio tensor.</param>
        /// <returns>The mono audio, the same instance when the audio is already mono.</returns>
        public static AudioTensor ToMono(this AudioTensor audioTensor)
        {
            if (audioTensor.Channels == 1)
                return audioTensor;

            // Mix
            var channels = audioTensor.Channels;
            var sampleCount = audioTensor.Samples;
            var source = audioTensor.Memory.Span;
            var samples = new float[sampleCount];
            for (int channel = 0; channel < channels; channel++)
            {
                var channelSamples = source.Slice(channel * sampleCount, sampleCount);
                for (int i = 0; i < sampleCount; i++)
                {
                    samples[i] += channelSamples[i] / channels;
                }
            }

            // Result
            return new AudioTensor(new Tensor<float>(samples, [1, sampleCount]), audioTensor.SampleRate);
        }
    }
}
