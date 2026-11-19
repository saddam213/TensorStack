using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace TensorStack.LlamaCpp.Templates
{
    /// <summary>
    /// Prompt formatter for Qwen 3.5, a port of the Qwen35_IT chat template (XML tool calls, trimmed content).
    /// </summary>
    internal class Qwen35Formatter : QwenFormatter
    {
        private const string ToolsHeader = "# Tools\n\nYou have access to the following functions:\n\n<tools>";
        private const string ToolsFooter = "\n</tools>\n\nIf you choose to call a function ONLY reply in the following format with NO suffix:\n\n<tool_call>\n<function=example_function_name>\n<parameter=example_parameter_1>\nvalue_1\n</parameter>\n<parameter=example_parameter_2>\nThis is the value for the second parameter\nthat can span\nmultiple lines\n</parameter>\n</function>\n</tool_call>\n\n<IMPORTANT>\nReminder:\n- Function calls MUST follow the specified format: an inner <function=...></function> block must be nested within <tool_call></tool_call> XML tags\n- Required parameters MUST be specified\n- You may provide optional reasoning for your function call in natural language BEFORE the function call, but NOT after\n- If there is no function call available, answer the question like normal with your current knowledge and do not tell the user about function calls\n</IMPORTANT>";

        /// <summary>
        /// Initializes a new instance of the <see cref="Qwen35Formatter"/> class.
        /// </summary>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        public Qwen35Formatter(string mediaMarker)
            : base(mediaMarker)
        {
        }

        /// <summary>
        /// Gets a value indicating whether scalar tool call arguments print as Python literals (True, False, None) rather than JSON.
        /// </summary>
        protected virtual bool UsesPythonLiterals => true;


        /// <summary>
        /// Appends the system block when tools are provided, the system content follows the tools.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tools">The tools.</param>
        /// <param name="hasSystem">if set to <c>true</c> the first message is a system message.</param>
        /// <param name="systemContent">The system content.</param>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        protected override void AppendToolSystem(StringBuilder builder, IReadOnlyList<string> tools, bool hasSystem, string systemContent, bool isThinkingEnabled)
        {
            var reasoningInstructions = GetReasoningInstructions(isThinkingEnabled);
            builder.Append("<|im_start|>system\n");
            if (reasoningInstructions.Length > 0)
                builder.Append(reasoningInstructions).Append("\n\n");

            builder.Append(ToolsHeader);
            AppendTools(builder, tools);
            builder.Append(ToolsFooter);
            if (hasSystem && systemContent.Length > 0)
                builder.Append("\n\n").Append(systemContent);

            builder.Append("<|im_end|>\n");
        }


        /// <summary>
        /// Appends the assistant reasoning and content.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="content">The content without the reasoning.</param>
        /// <param name="reasoning">The reasoning.</param>
        /// <param name="isAfterLastQuery">if set to <c>true</c> the message follows the last user query.</param>
        /// <param name="isLast">if set to <c>true</c> this is the last message.</param>
        protected override void AppendAssistantContent(StringBuilder builder, string content, string reasoning, bool isAfterLastQuery, bool isLast)
        {
            if (IsReasoningKept(isAfterLastQuery))
                builder.Append("<think>\n").Append(reasoning.Trim()).Append("\n</think>\n\n").Append(content);
            else
                builder.Append(content);
        }


        /// <summary>
        /// Appends a tool call as an XML function with parameters.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="toolCall">The tool call.</param>
        /// <param name="isFirst">if set to <c>true</c> this is the first tool call of the message.</param>
        /// <param name="content">The message content without the reasoning.</param>
        protected override void AppendToolCall(StringBuilder builder, PromptToolCall toolCall, bool isFirst, string content)
        {
            if (!isFirst)
                builder.Append('\n');
            else if (content.Trim().Length > 0)
                builder.Append("\n\n");

            builder.Append("<tool_call>\n<function=").Append(toolCall.Name).Append(">\n");
            if (toolCall.HasArguments && toolCall.Arguments.ValueKind == JsonValueKind.Object)
            {
                foreach (var argument in toolCall.Arguments.EnumerateObject())
                {
                    builder.Append("<parameter=").Append(argument.Name).Append(">\n");
                    builder.Append(GetArgumentText(argument.Value));
                    builder.Append("\n</parameter>\n");
                }
            }
            builder.Append("</function>\n</tool_call>");
        }


        /// <summary>
        /// Gets the reasoning instructions stated in the system block, Qwen 3.5 has none.
        /// </summary>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        protected virtual string GetReasoningInstructions(bool isThinkingEnabled)
        {
            return string.Empty;
        }


        /// <summary>
        /// Determines whether the reasoning of an assistant message is kept, Qwen 3.5 keeps the reasoning of the current turn only.
        /// </summary>
        /// <param name="isAfterLastQuery">if set to <c>true</c> the message follows the last user query.</param>
        protected virtual bool IsReasoningKept(bool isAfterLastQuery)
        {
            return isAfterLastQuery;
        }


        /// <summary>
        /// Gets the text of a tool call argument.
        /// </summary>
        /// <param name="value">The value.</param>
        private string GetArgumentText(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString();
            if (value.ValueKind == JsonValueKind.Object || value.ValueKind == JsonValueKind.Array)
                return ToJson(value);

            return ToText(value, UsesPythonLiterals);
        }
    }
}
