using System;

namespace TensorStack.Media.Audio
{
    /// <summary>
    /// Resamples audio with a Blackman-windowed sinc filter, when downsampling the filter also removes the frequencies the target rate cannot hold.
    /// </summary>
    public static class AudioResampler
    {
        private const int ZeroCrossings = 16;

        /// <summary>
        /// Resamples the samples of one channel.
        /// </summary>
        /// <param name="samples">The samples.</param>
        /// <param name="sourceRate">The source sample rate.</param>
        /// <param name="targetRate">The target sample rate.</param>
        /// <returns>The resampled samples, a copy of the input when the rates match.</returns>
        public static float[] Resample(ReadOnlySpan<float> samples, int sourceRate, int targetRate)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceRate);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetRate);
            if (sourceRate == targetRate || samples.Length == 0)
                return samples.ToArray();

            // Filter, the cutoff is the lower of the two Nyquist frequencies relative to the source rate
            var ratio = (double)sourceRate / targetRate;
            var cutoff = Math.Min(1.0, 1.0 / ratio);
            var halfWidth = ZeroCrossings / cutoff;

            // Resample
            var result = new float[GetSampleCount(samples.Length, sourceRate, targetRate)];
            for (int i = 0; i < result.Length; i++)
            {
                var center = i * ratio;
                var first = Math.Max(0, (int)Math.Ceiling(center - halfWidth));
                var last = Math.Min(samples.Length - 1, (int)Math.Floor(center + halfWidth));
                var sum = 0.0;
                var weightSum = 0.0;
                for (int j = first; j <= last; j++)
                {
                    var distance = j - center;
                    var weight = cutoff * Sinc(cutoff * distance) * BlackmanWindow(distance / halfWidth);
                    sum += samples[j] * weight;
                    weightSum += weight;
                }
                result[i] = weightSum > 0 ? (float)(sum / weightSum) : 0f;
            }
            return result;
        }


        /// <summary>
        /// Gets the number of samples after resampling.
        /// </summary>
        /// <param name="sampleCount">The source sample count.</param>
        /// <param name="sourceRate">The source sample rate.</param>
        /// <param name="targetRate">The target sample rate.</param>
        public static int GetSampleCount(int sampleCount, int sourceRate, int targetRate)
        {
            return (int)Math.Round((double)sampleCount * targetRate / sourceRate);
        }


        /// <summary>
        /// Gets the normalized sinc of the value.
        /// </summary>
        /// <param name="value">The value.</param>
        private static double Sinc(double value)
        {
            if (Math.Abs(value) < 1e-9)
                return 1.0;

            var x = Math.PI * value;
            return Math.Sin(x) / x;
        }


        /// <summary>
        /// Gets the Blackman window at the position, -1 to 1 across the window.
        /// </summary>
        /// <param name="position">The position.</param>
        private static double BlackmanWindow(double position)
        {
            if (Math.Abs(position) >= 1.0)
                return 0.0;

            var x = Math.PI * (position + 1.0);
            return 0.42 - 0.5 * Math.Cos(x) + 0.08 * Math.Cos(2.0 * x);
        }
    }
}
