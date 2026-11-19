using System.Text;

namespace TensorStack.LlamaCpp.Templates
{
    /// <summary>
    /// Prompt formatter for Qwen 3.8, a port of the Qwen38_IT chat template (Qwen 3.6 with reasoning effort instructions and preserved reasoning).
    /// </summary>
    internal sealed class Qwen38Formatter : Qwen36Formatter
    {
        private const string ReasoningInstructions = "Reasoning effort is set to xhigh. Please think carefully through the task, validate key assumptions, consider plausible alternatives, and prioritize correctness, consistency, and clarity in the final answer.";

        /// <summary>
        /// Initializes a new instance of the <see cref="Qwen38Formatter"/> class.
        /// </summary>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        public Qwen38Formatter(string mediaMarker)
            : base(mediaMarker)
        {
        }


        /// <summary>
        /// Appends the system block when no tools are provided, the reasoning instructions precede the system content and an empty system message is dropped.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="hasSystem">if set to <c>true</c> the first message is a system message.</param>
        /// <param name="systemContent">The system content.</param>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        protected override void AppendSystem(StringBuilder builder, bool hasSystem, string systemContent, bool isThinkingEnabled)
        {
            var reasoningInstructions = GetReasoningInstructions(isThinkingEnabled);
            if (hasSystem && systemContent.Length > 0)
            {
                builder.Append("<|im_start|>system\n");
                if (reasoningInstructions.Length > 0)
                    builder.Append(reasoningInstructions).Append("\n\n");

                builder.Append(systemContent).Append("<|im_end|>\n");
            }
            else if (reasoningInstructions.Length > 0)
            {
                builder.Append("<|im_start|>system\n").Append(reasoningInstructions).Append("<|im_end|>\n");
            }
        }


        /// <summary>
        /// Gets the reasoning instructions stated in the system block when thinking is enabled.
        /// </summary>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        protected override string GetReasoningInstructions(bool isThinkingEnabled)
        {
            return isThinkingEnabled ? ReasoningInstructions : string.Empty;
        }


        /// <summary>
        /// Determines whether the reasoning of an assistant message is kept, Qwen 3.8 preserves all reasoning.
        /// </summary>
        /// <param name="isAfterLastQuery">if set to <c>true</c> the message follows the last user query.</param>
        protected override bool IsReasoningKept(bool isAfterLastQuery)
        {
            return true;
        }
    }
}
