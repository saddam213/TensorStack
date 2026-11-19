namespace TensorStack.LlamaCpp.Templates
{
    /// <summary>
    /// Prompt formatter for Qwen 3.6, a port of the Qwen36_IT chat template (Qwen 3.5 with JSON scalar tool call arguments).
    /// </summary>
    internal class Qwen36Formatter : Qwen35Formatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Qwen36Formatter"/> class.
        /// </summary>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        public Qwen36Formatter(string mediaMarker)
            : base(mediaMarker)
        {
        }

        /// <summary>
        /// Gets a value indicating whether scalar tool call arguments print as Python literals, Qwen 3.6 prints JSON.
        /// </summary>
        protected override bool UsesPythonLiterals => false;
    }
}
