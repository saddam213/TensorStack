using System.Collections.Generic;

namespace TensorStack.AudioCpp.Common
{
    /// <summary>
    /// Options used to load an audio.cpp model.
    /// </summary>
    public sealed record AudioCppOptions
    {
        /// <summary>
        /// Gets or sets the model path, a GGUF file or a model directory.
        /// </summary>
        public string ModelPath { get; set; }

        /// <summary>
        /// Gets or sets the audio.cpp pipeline (model family e.g. ace_step, kokoro_tts), null to identify the model from its path.
        /// </summary>
        public string Pipeline { get; set; }

        /// <summary>
        /// Gets or sets the backend (vulkan, cuda, hip, cpu).
        /// </summary>
        public string Backend { get; set; } = "vulkan";

        /// <summary>
        /// Gets or sets the backend device index.
        /// </summary>
        public int Device { get; set; }

        /// <summary>
        /// Gets or sets the backend worker threads.
        /// </summary>
        public int Threads { get; set; } = 4;

        /// <summary>
        /// Gets or sets the weight storage type.
        /// </summary>
        public DataType DataType { get; set; } = DataType.Default;

        /// <summary>
        /// Gets or sets a value indicating whether model stages are released between phases to reduce peak memory.
        /// </summary>
        public bool IsMemorySaverEnabled { get; set; }

        /// <summary>
        /// Gets or sets the additional load options by audio.cpp name (--load-option).
        /// </summary>
        public Dictionary<string, string> LoadOptions { get; set; } = [];

        /// <summary>
        /// Gets or sets the additional session options by audio.cpp name (--session-option), these override the options above. Only options the model declares are applied.
        /// </summary>
        public Dictionary<string, string> SessionOptions { get; set; } = [];
    }
}
