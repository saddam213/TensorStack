using System;
using System.Collections.Generic;
using TensorStack.AudioCpp.Common;
using TensorStack.Common;
using TensorStack.Common.Tensor;

namespace TensorStack.AudioCpp.Native
{
    internal static unsafe class NativeApiExtensions
    {
        /// <summary>
        /// Creates a native options map, the caller must free it with audiocpp_options_free.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The options pointer, zero when there are no options.</returns>
        internal static nint ToUnmanaged(this IReadOnlyDictionary<string, string> options)
        {
            if (options == null || options.Count == 0)
                return nint.Zero;

            var nativeOptions = NativeApi.audiocpp_options_create();
            if (nativeOptions == nint.Zero)
                throw new OutOfMemoryException("Failed to create audio.cpp options");

            try
            {
                foreach (var option in options)
                {
                    if (option.Value == null)
                        continue;

                    NativeApi.ThrowIfFailed(NativeApi.audiocpp_options_set(nativeOptions, option.Key, option.Value), $"option set '{option.Key}'");
                }
                return nativeOptions;
            }
            catch (Exception)
            {
                NativeApi.audiocpp_options_free(nativeOptions);
                throw;
            }
        }


        /// <summary>
        /// Creates a native request, the caller must free it with audiocpp_request_free.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="requestOptions">The request options the model declares.</param>
        /// <returns>The request pointer.</returns>
        internal static nint ToUnmanaged(this AudioCppRequest request, IReadOnlyDictionary<string, string> requestOptions)
        {
            var nativeRequest = NativeApi.audiocpp_request_create();
            if (nativeRequest == nint.Zero)
                throw new OutOfMemoryException("Failed to create audio.cpp request");

            try
            {
                // Text, the language option is only written when the model declares it
                if (request.Text != null)
                {
                    NativeApi.ThrowIfFailed(NativeApi.audiocpp_request_set_text(nativeRequest, request.Text, null), "request set text");
                    if (!string.IsNullOrEmpty(request.Language))
                        NativeApi.ThrowIfFailed(NativeApi.audiocpp_request_set_text_language(nativeRequest, request.Language), "request set language");
                }

                // Audio
                if (request.Audio != null)
                {
                    var samples = request.Audio.ToInterleaved();
                    fixed (float* samplesPtr = samples)
                    {
                        NativeApi.ThrowIfFailed(NativeApi.audiocpp_request_set_audio(nativeRequest, samplesPtr, (nuint)request.Audio.Samples, request.Audio.SampleRate, request.Audio.Channels), "request set audio");
                    }
                }

                // Voice
                if (!string.IsNullOrEmpty(request.VoiceId))
                    NativeApi.ThrowIfFailed(NativeApi.audiocpp_request_set_voice_id(nativeRequest, request.VoiceId), "request set voice id");

                if (request.VoiceAudio != null)
                {
                    var samples = request.VoiceAudio.ToInterleaved();
                    fixed (float* samplesPtr = samples)
                    {
                        NativeApi.ThrowIfFailed(NativeApi.audiocpp_request_set_voice_audio(nativeRequest, samplesPtr, (nuint)request.VoiceAudio.Samples, request.VoiceAudio.SampleRate, request.VoiceAudio.Channels), "request set voice audio");
                    }
                }

                if (request.SpeakingRate > 0)
                    NativeApi.ThrowIfFailed(NativeApi.audiocpp_request_set_speaking_rate(nativeRequest, request.SpeakingRate), "request set speaking rate");

                // Options
                foreach (var option in requestOptions)
                {
                    NativeApi.ThrowIfFailed(NativeApi.audiocpp_request_set_option(nativeRequest, option.Key, option.Value), $"request set option '{option.Key}'");
                }
                return nativeRequest;
            }
            catch (Exception)
            {
                NativeApi.audiocpp_request_free(nativeRequest);
                throw;
            }
        }


        /// <summary>
        /// Copies the borrowed buffers of a native result to a managed result.
        /// </summary>
        /// <param name="nativeResult">The native result.</param>
        internal static AudioCppResult ToManaged(nint nativeResult)
        {
            AudioTensor audio = null;
            if (NativeApi.audiocpp_result_audio(nativeResult, out var samples, out var frames, out var sampleRate, out var channels) == NativeApi.StatusType.Ok && frames > 0 && channels > 0)
            {
                var interleaved = new ReadOnlySpan<float>((void*)samples, checked((int)frames * channels));
                audio = interleaved.ToAudioTensor((int)frames, channels, sampleRate);
            }

            string text = null;
            string language = null;
            if (NativeApi.audiocpp_result_text(nativeResult, out var textPtr, out var languagePtr) == NativeApi.StatusType.Ok)
            {
                text = NativeApi.PtrToString(textPtr);
                language = NativeApi.PtrToString(languagePtr);
            }
            return new AudioCppResult { Audio = audio, Text = text, Language = language };
        }


        /// <summary>
        /// Converts the [Channels, Samples] tensor to interleaved PCM.
        /// </summary>
        /// <param name="audio">The audio.</param>
        internal static float[] ToInterleaved(this AudioTensor audio)
        {
            var channels = audio.Channels;
            var frames = audio.Samples;
            var source = audio.Memory.Span;
            var result = new float[checked(frames * channels)];
            for (int c = 0; c < channels; c++)
            {
                var channel = source.Slice(c * frames, frames);
                for (int i = 0; i < frames; i++)
                {
                    result[i * channels + c] = channel[i];
                }
            }
            return result;
        }


        /// <summary>
        /// Converts interleaved PCM to a [Channels, Samples] tensor.
        /// </summary>
        /// <param name="samples">The interleaved samples.</param>
        /// <param name="frames">The frames per channel.</param>
        /// <param name="channels">The channels.</param>
        /// <param name="sampleRate">The sample rate.</param>
        internal static AudioTensor ToAudioTensor(this ReadOnlySpan<float> samples, int frames, int channels, int sampleRate)
        {
            var result = new Tensor<float>([channels, frames]);
            var destination = result.Memory.Span;
            for (int c = 0; c < channels; c++)
            {
                var channel = destination.Slice(c * frames, frames);
                for (int i = 0; i < frames; i++)
                {
                    channel[i] = samples[i * channels + c];
                }
            }
            return result.AsAudioTensor(sampleRate);
        }
    }
}
