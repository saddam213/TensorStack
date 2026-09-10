// Copyright (c) Adam Clark. All rights reserved.
// Licensed under the Apache 2.0 License.
using System;
using System.Collections.Generic;

namespace TensorStack.Common.Tensor
{
    /// <summary>
    /// Class to handle audio in Tensor format
    /// Implements the <see cref="Tensor{float}" />
    /// </summary>
    /// <seealso cref="Tensor{float}" />
    public class AudioTensor : Tensor<float>
    {
        protected int _sampleRate;
        protected TimeSpan _duration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioTensor"/> class.
        /// </summary>
        /// <param name="tensor">The tensor.</param>
        /// <param name="sampleRate">The source audio sample rate.</param>
        public AudioTensor(Tensor<float> tensor, int sampleRate = 16000)
            : base(tensor.Memory, tensor.Dimensions)
        {
            _sampleRate = sampleRate;
            ThrowIfInvalid();
        }

        /// <summary>
        /// Gets the audio channel count (Mono, Stereo etc)
        /// </summary>
        public int Channels => Dimensions[0];

        /// <summary>
        /// Gets the sample count.
        /// </summary>
        public int Samples => Dimensions[1];

        /// <summary>
        /// Gets the sample rate.
        /// </summary>
        public int SampleRate => _sampleRate;

        /// <summary>
        /// Gets the duration.
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromSeconds((double)Samples / SampleRate);


        /// <summary>
        /// Splits the Audio specified second chunks.
        /// </summary>
        /// <param name="seconds">The seconds.</param>
        public IEnumerable<AudioTensor> Chunk(float seconds)
        {
            var sampleRate = (float)SampleRate;
            var samplesPerChunk = (int)Math.Round(seconds * sampleRate);
            for (int start = 0; start < Samples; start += samplesPerChunk)
            {
                var length = Math.Min(samplesPerChunk, Samples - start);
                yield return GetSequence(start / sampleRate, length / sampleRate);
            }
        }


        /// <summary>
        /// Gets a chunk fo audio.
        /// </summary>
        /// <param name="lengthSeconds">The length seconds.</param>
        /// <returns>AudioTensor.</returns>
        public AudioTensor GetSequence(float lengthSeconds)
        {
            return GetSequence(0, lengthSeconds);
        }


        /// <summary>
        /// Gets a chunk fo audio.
        /// </summary>
        /// <param name="startSeconds">The start seconds.</param>
        /// <param name="lengthSeconds">The length seconds.</param>
        /// <returns>AudioTensor.</returns>
        public AudioTensor GetSequence(float startSeconds, float lengthSeconds)
        {
            var start = (int)Math.Round(startSeconds * SampleRate) * Channels;
            var count = (int)Math.Round(lengthSeconds * SampleRate) * Channels;
            var samples = Memory.Span[start..(start + count)].ToArray();
            return new Tensor<float>(samples, [Channels, samples.Length / Channels]).AsAudioTensor(SampleRate);
        }


        /// <summary>
        /// Throws if Dimensions are invalid.
        /// </summary>
        protected void ThrowIfInvalid()
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Samples, 0, nameof(Samples));
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Channels, 0, nameof(Channels));
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(SampleRate, 0, nameof(SampleRate));
        }
    }
}
