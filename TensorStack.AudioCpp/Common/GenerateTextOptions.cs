using System.Collections.Generic;
using TensorStack.Common.Tensor;

namespace TensorStack.AudioCpp.Common
{
    /// <summary>
    /// Text generation options (speech recognition).
    /// Values left at their default are not sent, so the audio.cpp model default applies, and only the options the loaded model supports are used.
    /// </summary>
    public sealed record GenerateTextOptions
    {
        /// <summary>
        /// Gets or sets the seed, negative for a random seed.
        /// </summary>
        public int Seed { get; set; } = -1;

        /// <summary>
        /// Gets or sets the prompt.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the maximum tokens to generate.
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether sampling is enabled, the sampling options below are only sent when it is.
        /// </summary>
        public bool IsSamplingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the beams.
        /// </summary>
        public int Beams { get; set; }

        /// <summary>
        /// Gets or sets the sampling temperature.
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// Gets or sets the top-k.
        /// </summary>
        public int TopK { get; set; }

        /// <summary>
        /// Gets or sets the top-p.
        /// </summary>
        public float TopP { get; set; }

        /// <summary>
        /// Gets or sets the repetition penalty.
        /// </summary>
        public float RepetitionPenalty { get; set; }

        /// <summary>
        /// Gets or sets the audio chunk duration in seconds.
        /// </summary>
        public int ChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the additional request options by audio.cpp name (--request-option), these override the options above.
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = [];

        /// <summary>
        /// Gets or sets the input audios, the audio to transcribe.
        /// </summary>
        public List<AudioTensor> InputAudios { get; set; } = [];
    }
}
