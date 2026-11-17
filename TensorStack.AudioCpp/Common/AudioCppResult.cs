using TensorStack.Common.Tensor;

namespace TensorStack.AudioCpp.Common
{
    /// <summary>
    /// The result of an audio.cpp task.
    /// </summary>
    internal sealed record AudioCppResult
    {
        /// <summary>
        /// Gets the audio output, null if the task produced no audio.
        /// </summary>
        public AudioTensor Audio { get; init; }

        /// <summary>
        /// Gets the text output, null if the task produced no text.
        /// </summary>
        public string Text { get; init; }

        /// <summary>
        /// Gets the detected language.
        /// </summary>
        public string Language { get; init; }
    }
}
