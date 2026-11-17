using System.Collections.Generic;
using TensorStack.Common.Tensor;

namespace TensorStack.AudioCpp.Common
{
    /// <summary>
    /// Audio generation options (music, sound effects, speech).
    /// Values left at their default are not sent, so the audio.cpp model default applies, and only the options the loaded model supports are used.
    /// </summary>
    public sealed record GenerateAudioOptions
    {
        /// <summary>
        /// Gets or sets the seed, negative for a random seed.
        /// </summary>
        public int Seed { get; set; } = -1;

        /// <summary>
        /// Gets or sets the prompt: the caption or style for music, the description for sound effects, the text to speak for speech.
        /// </summary>
        public string Prompt { get; set; }

        /// <summary>
        /// Gets or sets the second prompt, the lyrics for music.
        /// </summary>
        public string Prompt2 { get; set; }

        /// <summary>
        /// Gets or sets the negative prompt.
        /// </summary>
        public string NegativePrompt { get; set; }

        /// <summary>
        /// Gets or sets the guidance scale.
        /// </summary>
        public float GuidanceScale { get; set; }

        /// <summary>
        /// Gets or sets the second guidance scale, the autoregressive guidance of MiniMax Music.
        /// </summary>
        public float GuidanceScale2 { get; set; }

        /// <summary>
        /// Gets or sets the inference steps.
        /// </summary>
        public int Steps { get; set; }

        /// <summary>
        /// Gets or sets the strength of the generation over the input audio, 0 to 1.
        /// </summary>
        public float Strength { get; set; } = 1;

        /// <summary>
        /// Gets or sets the duration in seconds.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the instruction.
        /// </summary>
        public string Instruction { get; set; }

        /// <summary>
        /// Gets or sets the task: the route for music (text2music, cover, repaint, full, melody etc), the built-in voice for speech.
        /// </summary>
        public string Task { get; set; }

        /// <summary>
        /// Gets or sets the beats per minute.
        /// </summary>
        public int Bpm { get; set; }

        /// <summary>
        /// Gets or sets the musical key.
        /// </summary>
        public string Keyscale { get; set; }

        /// <summary>
        /// Gets or sets the target track name.
        /// </summary>
        public string TrackName { get; set; }

        /// <summary>
        /// Gets or sets the time signature.
        /// </summary>
        public string TimeSignature { get; set; }

        /// <summary>
        /// Gets or sets the speech speed multiplier.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// Gets or sets the additional request options by audio.cpp name (--request-option), these override the options above.
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = [];

        /// <summary>
        /// Gets or sets the input audios: the source audio for music and sound effects, the speaker reference for speech.
        /// </summary>
        public List<AudioTensor> InputAudios { get; set; } = [];
    }
}
