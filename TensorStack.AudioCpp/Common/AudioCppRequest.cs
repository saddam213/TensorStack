using System.Collections.Generic;
using TensorStack.Common.Tensor;

namespace TensorStack.AudioCpp.Common
{
    /// <summary>
    /// A single audio.cpp task request.
    /// </summary>
    internal sealed record AudioCppRequest
    {
        /// <summary>
        /// Gets the candidate tasks in order of preference (gen, tts, clon, asr), the first the model supports is used.
        /// </summary>
        public string[] Tasks { get; init; }

        /// <summary>
        /// Gets the text input.
        /// </summary>
        public string Text { get; init; }

        /// <summary>
        /// Gets the language code.
        /// </summary>
        public string Language { get; init; }

        /// <summary>
        /// Gets the input audio.
        /// </summary>
        public AudioTensor Audio { get; init; }

        /// <summary>
        /// Gets the speaker reference audio.
        /// </summary>
        public AudioTensor VoiceAudio { get; init; }

        /// <summary>
        /// Gets the built-in voice id.
        /// </summary>
        public string VoiceId { get; init; }

        /// <summary>
        /// Gets the speaking rate, zero to use the model default.
        /// </summary>
        public float SpeakingRate { get; init; }

        /// <summary>
        /// Gets the request options (--request-option), only options the model declares are applied.
        /// </summary>
        public Dictionary<string, string> Options { get; init; } = [];

        /// <summary>
        /// Gets the session options (--session-option) for this request, the session is recreated when they change.
        /// </summary>
        public Dictionary<string, string> SessionOptions { get; init; } = [];
    }
}
