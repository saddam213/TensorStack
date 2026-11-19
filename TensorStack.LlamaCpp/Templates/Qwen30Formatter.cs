using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TensorStack.LlamaCpp.Common;

namespace TensorStack.LlamaCpp.Templates
{
    /// <summary>
    /// Prompt formatter for Qwen 3.0, a port of the Qwen30_IT chat template (JSON tool calls, untrimmed content).
    /// </summary>
    internal sealed class Qwen30Formatter : QwenFormatter
    {
        private const string ToolsHeader = "# Tools\n\nYou may call one or more functions to assist with the user query.\n\nYou are provided with function signatures within <tools></tools> XML tags:\n<tools>";
        private const string ToolsFooter = "\n</tools>\n\nFor each function call, return a json object with function name and arguments within <tool_call></tool_call> XML tags:\n<tool_call>\n{\"name\": <function-name>, \"arguments\": <args-json-object>}\n</tool_call><|im_end|>\n";

        /// <summary>
        /// Initializes a new instance of the <see cref="Qwen30Formatter"/> class.
        /// </summary>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        public Qwen30Formatter(string mediaMarker)
            : base(mediaMarker)
        {
        }

        /// <summary>
        /// Gets a value indicating whether a tool response at the start of the conversation opens a user turn.
        /// </summary>
        protected override bool OpensUserTurnForLeadingToolResponse => true;


        /// <summary>
        /// Appends the system block when tools are provided, the tools follow the system content.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tools">The tools.</param>
        /// <param name="hasSystem">if set to <c>true</c> the first message is a system message.</param>
        /// <param name="systemContent">The system content.</param>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        protected override void AppendToolSystem(StringBuilder builder, IReadOnlyList<string> tools, bool hasSystem, string systemContent, bool isThinkingEnabled)
        {
            builder.Append("<|im_start|>system\n");
            if (hasSystem)
                builder.Append(systemContent).Append("\n\n");

            builder.Append(ToolsHeader);
            AppendTools(builder, tools);
            builder.Append(ToolsFooter);
        }


        /// <summary>
        /// Appends a system message that is not the first message, Qwen 3.0 keeps these in place.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="message">The message.</param>
        protected override void AppendLaterSystem(StringBuilder builder, ConversationMessage message)
        {
            builder.Append("<|im_start|>system\n").Append(GetText(message)).Append("<|im_end|>\n");
        }


        /// <summary>
        /// Appends the assistant reasoning and content, the reasoning is kept after the last query when it exists or the message is the last.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="content">The content without the reasoning.</param>
        /// <param name="reasoning">The reasoning.</param>
        /// <param name="isAfterLastQuery">if set to <c>true</c> the message follows the last user query.</param>
        /// <param name="isLast">if set to <c>true</c> this is the last message.</param>
        protected override void AppendAssistantContent(StringBuilder builder, string content, string reasoning, bool isAfterLastQuery, bool isLast)
        {
            if (isAfterLastQuery && (isLast || reasoning.Length > 0))
                builder.Append("<think>\n").Append(reasoning.Trim('\n')).Append("\n</think>\n\n").Append(content.TrimStart('\n'));
            else
                builder.Append(content);
        }


        /// <summary>
        /// Appends a tool call as a JSON object.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="toolCall">The tool call.</param>
        /// <param name="isFirst">if set to <c>true</c> this is the first tool call of the message.</param>
        /// <param name="content">The message content without the reasoning.</param>
        protected override void AppendToolCall(StringBuilder builder, PromptToolCall toolCall, bool isFirst, string content)
        {
            if (!isFirst || content.Length > 0)
                builder.Append('\n');

            builder.Append("<tool_call>\n{\"name\": \"").Append(toolCall.Name).Append("\", \"arguments\": ");
            if (!toolCall.HasArguments)
                builder.Append("{}");
            else if (toolCall.Arguments.ValueKind == JsonValueKind.String)
                builder.Append(toolCall.Arguments.GetString());
            else
                builder.Append(ToJson(toolCall.Arguments));

            builder.Append("}\n</tool_call>");
        }


        /// <summary>
        /// Normalizes message content, Qwen 3.0 keeps the content as it is.
        /// </summary>
        /// <param name="content">The content.</param>
        protected override string NormalizeContent(string content)
        {
            return content;
        }
    }
}
