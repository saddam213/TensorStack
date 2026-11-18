using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TensorStack.LlamaCpp.Common;

namespace TensorStack.LlamaCpp.Templates
{
    /// <summary>
    /// Prompt formatter for the Qwen 3.x family (ChatML), a port of the Qwen30_IT, Qwen35_IT, Qwen36_IT and Qwen38_IT chat templates.
    /// </summary>
    internal sealed class QwenFormatter : PromptFormatter
    {
        private const string ToolResponseStart = "<tool_response>";
        private const string ToolResponseEnd = "</tool_response>";
        private const string ToolsHeader30 = "# Tools\n\nYou may call one or more functions to assist with the user query.\n\nYou are provided with function signatures within <tools></tools> XML tags:\n<tools>";
        private const string ToolsFooter30 = "\n</tools>\n\nFor each function call, return a json object with function name and arguments within <tool_call></tool_call> XML tags:\n<tool_call>\n{\"name\": <function-name>, \"arguments\": <args-json-object>}\n</tool_call><|im_end|>\n";
        private const string ToolsHeader35 = "# Tools\n\nYou have access to the following functions:\n\n<tools>";
        private const string ToolsFooter35 = "\n</tools>\n\nIf you choose to call a function ONLY reply in the following format with NO suffix:\n\n<tool_call>\n<function=example_function_name>\n<parameter=example_parameter_1>\nvalue_1\n</parameter>\n<parameter=example_parameter_2>\nThis is the value for the second parameter\nthat can span\nmultiple lines\n</parameter>\n</function>\n</tool_call>\n\n<IMPORTANT>\nReminder:\n- Function calls MUST follow the specified format: an inner <function=...></function> block must be nested within <tool_call></tool_call> XML tags\n- Required parameters MUST be specified\n- You may provide optional reasoning for your function call in natural language BEFORE the function call, but NOT after\n- If there is no function call available, answer the question like normal with your current knowledge and do not tell the user about function calls\n</IMPORTANT>";
        private const string ReasoningInstructions38 = "Reasoning effort is set to xhigh. Please think carefully through the task, validate key assumptions, consider plausible alternatives, and prioritize correctness, consistency, and clarity in the final answer.";
        private static readonly string[] _stopTokens = ["<|im_end|>", "<|endoftext|>"];
        private static readonly string[] _reservedTokens = ["<|im_start|>", "<|im_end|>", "<|endoftext|>", "<|vision_start|>", "<|vision_end|>", "<|image_pad|>", "<|video_pad|>"];
        private readonly PromptTemplate _template;

        /// <summary>
        /// Initializes a new instance of the <see cref="QwenFormatter"/> class.
        /// </summary>
        /// <param name="template">The Qwen template version.</param>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        public QwenFormatter(PromptTemplate template, string mediaMarker)
            : base(mediaMarker)
        {
            _template = template;
        }

        /// <summary>
        /// Gets the tokens that end the generation in addition to the end tokens the model declares.
        /// </summary>
        public override IReadOnlyList<string> StopTokens => _stopTokens;

        /// <summary>
        /// Gets the tokens of the prompt format, these are removed from message content so content can not alter the prompt structure.
        /// </summary>
        public override IReadOnlyList<string> ReservedTokens => _reservedTokens;


        /// <summary>
        /// Formats the conversation.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="tools">The tools, each an OpenAI style function JSON schema.</param>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        public override PromptResult Format(IReadOnlyList<ConversationMessage> messages, IReadOnlyList<string> tools, bool isThinkingEnabled)
        {
            var builder = new StringBuilder();
            var media = new List<PromptMedia>();
            var isLegacy = _template == PromptTemplate.Qwen30;
            var hasSystem = messages.Count > 0 && messages[0].Role == ConversationRole.System;
            var systemContent = hasSystem ? GetText(messages[0], isLegacy) : string.Empty;

            // System and Tools
            if (isLegacy)
                AppendSystem30(builder, tools, hasSystem, systemContent);
            else
                AppendSystem35(builder, tools, hasSystem, systemContent, isThinkingEnabled);

            // Messages
            var lastQueryIndex = GetLastQueryIndex(messages, isLegacy);
            for (int i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                var isLast = i == messages.Count - 1;
                switch (message.Role)
                {
                    case ConversationRole.System:
                        if (isLegacy && i > 0)
                            builder.Append("<|im_start|>system\n").Append(GetText(message, isLegacy)).Append("<|im_end|>\n");
                        break;
                    case ConversationRole.User:
                        builder.Append("<|im_start|>user\n").Append(GetContent(message, media, isLegacy)).Append("<|im_end|>\n");
                        break;
                    case ConversationRole.Assistant:
                        AppendAssistant(builder, message, i > lastQueryIndex, isLast);
                        break;
                    case ConversationRole.Tool:
                        AppendToolResponse(builder, messages, i, isLegacy);
                        break;
                }
            }

            // Generation
            builder.Append("<|im_start|>assistant\n");
            if (!isThinkingEnabled)
                builder.Append("<think>\n\n</think>\n\n");

            return new PromptResult(builder.ToString(), media);
        }


        /// <summary>
        /// Appends the Qwen 3.0 system block, the tools follow the system content.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tools">The tools.</param>
        /// <param name="hasSystem">if set to <c>true</c> the first message is a system message.</param>
        /// <param name="systemContent">The system content.</param>
        private static void AppendSystem30(StringBuilder builder, IReadOnlyList<string> tools, bool hasSystem, string systemContent)
        {
            if (tools != null && tools.Count > 0)
            {
                builder.Append("<|im_start|>system\n");
                if (hasSystem)
                    builder.Append(systemContent).Append("\n\n");

                builder.Append(ToolsHeader30);
                AppendTools(builder, tools);
                builder.Append(ToolsFooter30);
            }
            else if (hasSystem)
            {
                builder.Append("<|im_start|>system\n").Append(systemContent).Append("<|im_end|>\n");
            }
        }


        /// <summary>
        /// Appends the Qwen 3.5+ system block, the system content follows the tools.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tools">The tools.</param>
        /// <param name="hasSystem">if set to <c>true</c> the first message is a system message.</param>
        /// <param name="systemContent">The system content.</param>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        private void AppendSystem35(StringBuilder builder, IReadOnlyList<string> tools, bool hasSystem, string systemContent, bool isThinkingEnabled)
        {
            // Qwen 3.8 states the reasoning effort in the system block
            var reasoningInstructions = _template == PromptTemplate.Qwen38 && isThinkingEnabled ? ReasoningInstructions38 : string.Empty;
            if (tools != null && tools.Count > 0)
            {
                builder.Append("<|im_start|>system\n");
                if (reasoningInstructions.Length > 0)
                    builder.Append(reasoningInstructions).Append("\n\n");

                builder.Append(ToolsHeader35);
                AppendTools(builder, tools);
                builder.Append(ToolsFooter35);
                if (hasSystem && systemContent.Length > 0)
                    builder.Append("\n\n").Append(systemContent);

                builder.Append("<|im_end|>\n");
                return;
            }

            if (_template == PromptTemplate.Qwen38)
            {
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
            else if (hasSystem)
            {
                builder.Append("<|im_start|>system\n").Append(systemContent).Append("<|im_end|>\n");
            }
        }


        /// <summary>
        /// Appends the tool schemas, one JSON object per line.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tools">The tools.</param>
        private static void AppendTools(StringBuilder builder, IReadOnlyList<string> tools)
        {
            foreach (var tool in tools)
            {
                using (var document = JsonDocument.Parse(tool))
                {
                    builder.Append('\n').Append(ToJson(document.RootElement));
                }
            }
        }


        /// <summary>
        /// Appends an assistant message.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="message">The message.</param>
        /// <param name="isAfterLastQuery">if set to <c>true</c> the message follows the last user query.</param>
        /// <param name="isLast">if set to <c>true</c> this is the last message.</param>
        private void AppendAssistant(StringBuilder builder, ConversationMessage message, bool isAfterLastQuery, bool isLast)
        {
            var isLegacy = _template == PromptTemplate.Qwen30;
            var content = SplitReasoning(GetText(message, isLegacy), out var reasoning);
            builder.Append("<|im_start|>assistant\n");
            if (isLegacy)
            {
                if (isAfterLastQuery && (isLast || reasoning.Length > 0))
                    builder.Append("<think>\n").Append(reasoning.Trim('\n')).Append("\n</think>\n\n").Append(content.TrimStart('\n'));
                else
                    builder.Append(content);
            }
            else
            {
                // Qwen 3.5 keeps the reasoning of the current turn only, Qwen 3.8 preserves all reasoning
                if (isAfterLastQuery || _template == PromptTemplate.Qwen38)
                    builder.Append("<think>\n").Append(reasoning.Trim()).Append("\n</think>\n\n").Append(content);
                else
                    builder.Append(content);
            }

            // Tool Calls
            var toolCalls = ParseToolCalls(message);
            for (int i = 0; i < toolCalls.Count; i++)
            {
                if (isLegacy)
                    AppendToolCall30(builder, toolCalls[i], i == 0, content.Length > 0);
                else
                    AppendToolCall35(builder, toolCalls[i], i == 0, content.Trim().Length > 0);
            }
            builder.Append("<|im_end|>\n");
        }


        /// <summary>
        /// Appends a Qwen 3.0 tool call (JSON).
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="toolCall">The tool call.</param>
        /// <param name="isFirst">if set to <c>true</c> this is the first tool call of the message.</param>
        /// <param name="hasContent">if set to <c>true</c> the message has content.</param>
        private static void AppendToolCall30(StringBuilder builder, PromptToolCall toolCall, bool isFirst, bool hasContent)
        {
            if (!isFirst || hasContent)
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
        /// Appends a Qwen 3.5+ tool call (XML function and parameters).
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="toolCall">The tool call.</param>
        /// <param name="isFirst">if set to <c>true</c> this is the first tool call of the message.</param>
        /// <param name="hasContent">if set to <c>true</c> the message has content.</param>
        private void AppendToolCall35(StringBuilder builder, PromptToolCall toolCall, bool isFirst, bool hasContent)
        {
            if (!isFirst)
                builder.Append('\n');
            else if (hasContent)
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
        /// Gets the text of a tool call argument, Qwen 3.5 prints scalars as Python literals while later versions print JSON.
        /// </summary>
        /// <param name="value">The value.</param>
        private string GetArgumentText(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString();
            if (value.ValueKind == JsonValueKind.Object || value.ValueKind == JsonValueKind.Array)
                return ToJson(value);

            return ToText(value, _template == PromptTemplate.Qwen35);
        }


        /// <summary>
        /// Appends a tool response, consecutive tool responses share one user turn.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="messages">The messages.</param>
        /// <param name="index">The index.</param>
        /// <param name="isLegacy">if set to <c>true</c> the Qwen 3.0 template is used.</param>
        private static void AppendToolResponse(StringBuilder builder, IReadOnlyList<ConversationMessage> messages, int index, bool isLegacy)
        {
            var isFirst = index == 0;
            var isLast = index == messages.Count - 1;
            var isPreviousTool = !isFirst && messages[index - 1].Role == ConversationRole.Tool;
            var isNextTool = !isLast && messages[index + 1].Role == ConversationRole.Tool;

            // Qwen 3.0 opens the user turn for a leading tool response, Qwen 3.5+ does not
            if (isFirst ? isLegacy : !isPreviousTool)
                builder.Append("<|im_start|>user");

            builder.Append("\n<tool_response>\n").Append(GetText(messages[index], isLegacy)).Append("\n</tool_response>");
            if (isLast || !isNextTool)
                builder.Append("<|im_end|>\n");
        }


        /// <summary>
        /// Gets the index of the last user query, user messages that only carry tool responses are not queries.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="isLegacy">if set to <c>true</c> the Qwen 3.0 template is used.</param>
        private static int GetLastQueryIndex(IReadOnlyList<ConversationMessage> messages, bool isLegacy)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].Role != ConversationRole.User)
                    continue;

                var content = GetText(messages[i], isLegacy);
                if (!(content.StartsWith(ToolResponseStart, StringComparison.Ordinal) && content.EndsWith(ToolResponseEnd, StringComparison.Ordinal)))
                    return i;
            }
            return messages.Count - 1;
        }


        /// <summary>
        /// Gets the message text, Qwen 3.5+ trims the content.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="isLegacy">if set to <c>true</c> the Qwen 3.0 template is used.</param>
        private static string GetText(ConversationMessage message, bool isLegacy)
        {
            var content = message.Content ?? string.Empty;
            return isLegacy ? content : content.Trim();
        }


        /// <summary>
        /// Gets the message content with its media, images precede the text and audios follow it.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="media">The media.</param>
        /// <param name="isLegacy">if set to <c>true</c> the Qwen 3.0 template is used.</param>
        private string GetContent(ConversationMessage message, List<PromptMedia> media, bool isLegacy)
        {
            var builder = new StringBuilder();
            AppendImages(builder, message, media);
            builder.Append(message.Content ?? string.Empty);
            AppendAudios(builder, message, media);
            return isLegacy ? builder.ToString() : builder.ToString().Trim();
        }
    }
}
