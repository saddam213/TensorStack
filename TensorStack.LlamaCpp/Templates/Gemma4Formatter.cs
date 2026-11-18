using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using TensorStack.LlamaCpp.Common;

namespace TensorStack.LlamaCpp.Templates
{
    /// <summary>
    /// Prompt formatter for Gemma 4, a port of the Gemma4_IT chat template. The BOS token is added by the tokenizer.
    /// </summary>
    internal sealed class Gemma4Formatter : PromptFormatter
    {
        private const string Quote = "<|\"|>";
        private const string ThinkStart = "<think>";
        private const string ThinkEnd = "</think>";
        private static readonly string[] _standardKeys = ["description", "type", "properties", "required", "nullable"];
        private static readonly string[] _stopTokens = ["<turn|>", "<|tool_response>"];
        private static readonly string[] _reservedTokens =
        [
            "<|turn>", "<turn|>", "<|think|>", "<|channel>", "<channel|>", "<|tool>", "<tool|>", "<|tool_call>", "<tool_call|>",
            "<|tool_response>", "<tool_response|>", "<|image|>", "<|audio|>", "<|video|>", Quote
        ];

        /// <summary>
        /// Initializes a new instance of the <see cref="Gemma4Formatter"/> class.
        /// </summary>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        public Gemma4Formatter(string mediaMarker)
            : base(mediaMarker) { }

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
            var hasTools = tools != null && tools.Count > 0;
            var hasSystem = messages.Count > 0 && messages[0].Role == ConversationRole.System;
            var previousMessageType = MessageType.None;
            var previousRole = default(ConversationRole?);

            // System and Tools
            if (isThinkingEnabled || hasTools || hasSystem)
            {
                builder.Append("<|turn>system\n");
                if (isThinkingEnabled)
                {
                    builder.Append("<|think|>\n");
                    previousMessageType = MessageType.Think;
                }

                if (hasSystem)
                    builder.Append((messages[0].Content ?? string.Empty).Trim());

                if (hasTools)
                {
                    foreach (var tool in tools)
                    {
                        builder.Append("<|tool>").Append(GetFunctionDeclaration(tool).Trim()).Append("<tool|>");
                    }
                    previousMessageType = MessageType.Tool;
                }
                builder.Append("<turn|>\n");
            }

            // Messages
            var loopMessages = hasSystem ? messages.Skip(1).ToList() : messages.ToList();
            for (int i = 0; i < loopMessages.Count; i++)
            {
                var message = loopMessages[i];
                if (message.Role == ConversationRole.Tool)
                    continue;

                previousMessageType = MessageType.None;
                var isModel = message.Role == ConversationRole.Assistant;
                if (!(isModel && previousRole == ConversationRole.Assistant))
                    builder.Append("<|turn>").Append(GetRoleName(message.Role)).Append('\n');

                // Tool Calls
                var toolCalls = ParseToolCalls(message);
                foreach (var toolCall in toolCalls)
                {
                    builder.Append("<|tool_call>call:").Append(toolCall.Name).Append('{');
                    if (toolCall.HasArguments && toolCall.Arguments.ValueKind == JsonValueKind.Object)
                        builder.Append(string.Join(',', GetSortedProperties(toolCall.Arguments).Select(x => $"{x.Name}:{FormatArgument(x.Value, false)}")));

                    builder.Append("}<tool_call|>");
                    previousMessageType = MessageType.ToolCall;
                }

                // Tool Responses, the tool messages that directly follow the tool calls
                var hasToolResponse = false;
                if (toolCalls.Count > 0)
                {
                    var responseIndex = 0;
                    for (int k = i + 1; k < loopMessages.Count && loopMessages[k].Role == ConversationRole.Tool; k++)
                    {
                        var toolName = toolCalls[Math.Min(responseIndex++, toolCalls.Count - 1)].Name;
                        builder.Append("<|tool_response>response:").Append(toolName).Append("{value:");
                        builder.Append(Quote).Append(loopMessages[k].Content ?? string.Empty).Append(Quote);
                        builder.Append("}<tool_response|>");
                        hasToolResponse = true;
                        previousMessageType = MessageType.ToolResponse;
                    }
                }

                // Content
                var content = GetContent(message, media, isModel);
                builder.Append(content);

                // Turn end, a model turn continues into the next assistant message
                var nextRole = GetNextRole(loopMessages, i);
                var hasContent = content.Trim().Length > 0;
                var isContinued = isModel && nextRole == ConversationRole.Assistant && (toolCalls.Count == 0 || hasToolResponse);
                if (previousMessageType == MessageType.ToolCall && !hasToolResponse)
                    builder.Append("<|tool_response>");
                else if (!isContinued && !(hasToolResponse && !hasContent && nextRole == null))
                    builder.Append("<turn|>\n");

                previousRole = message.Role;
            }

            // Generation
            if (previousMessageType != MessageType.ToolResponse && previousMessageType != MessageType.ToolCall)
                builder.Append("<|turn>model\n");
            else if (previousMessageType == MessageType.ToolResponse && isThinkingEnabled)
                builder.Append("<|channel>\n");

            return new PromptResult(builder.ToString(), media);
        }


        /// <summary>
        /// Gets the message content with its media, images precede the text and audios follow it.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="media">The media.</param>
        /// <param name="isModel">if set to <c>true</c> the message is a model message, its thinking is removed.</param>
        private string GetContent(ConversationMessage message, List<PromptMedia> media, bool isModel)
        {
            var builder = new StringBuilder();
            var content = message.Content ?? string.Empty;
            AppendImages(builder, message, media);
            builder.Append(isModel ? StripThinking(content) : content.Trim());
            AppendAudios(builder, message, media);
            return builder.ToString();
        }


        /// <summary>
        /// Removes the thinking blocks from model content.
        /// </summary>
        /// <param name="content">The content.</param>
        private static string StripThinking(string content)
        {
            var builder = new StringBuilder();
            foreach (var part in content.Split(ThinkEnd))
            {
                var startIndex = part.IndexOf(ThinkStart, StringComparison.Ordinal);
                builder.Append(startIndex >= 0 ? part.Substring(0, startIndex) : part);
            }
            return builder.ToString().Trim();
        }


        /// <summary>
        /// Gets the role of the next message that is not a tool message, null if there is none.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="index">The index.</param>
        private static ConversationRole? GetNextRole(List<ConversationMessage> messages, int index)
        {
            for (int i = index + 1; i < messages.Count; i++)
            {
                if (messages[i].Role != ConversationRole.Tool)
                    return messages[i].Role;
            }
            return null;
        }


        /// <summary>
        /// Gets the turn name of the role.
        /// </summary>
        /// <param name="role">The role.</param>
        private static string GetRoleName(ConversationRole role)
        {
            return role switch
            {
                ConversationRole.Assistant => "model",
                ConversationRole.System => "system",
                _ => "user"
            };
        }


        /// <summary>
        /// Gets the function declaration of a tool.
        /// </summary>
        /// <param name="tool">The tool, an OpenAI style function JSON schema.</param>
        private static string GetFunctionDeclaration(string tool)
        {
            using (var document = JsonDocument.Parse(tool))
            {
                var builder = new StringBuilder();
                var function = document.RootElement.TryGetProperty("function", out var inner) ? inner : document.RootElement;
                builder.Append("declaration:").Append(GetString(function, "name"));
                builder.Append("{description:").Append(Quote).Append(GetString(function, "description")).Append(Quote);
                if (function.TryGetProperty("parameters", out var parameters) && IsTruthy(parameters))
                {
                    builder.Append(",parameters:{");
                    var hasRequired = parameters.TryGetProperty("required", out var required) && IsTruthy(required);
                    if (parameters.TryGetProperty("properties", out var properties) && IsTruthy(properties))
                        builder.Append("properties:{").Append(FormatParameters(properties, false)).Append("},");

                    if (hasRequired)
                        builder.Append("required:[").Append(FormatRequired(required)).Append("],");

                    var type = GetString(parameters, "type");
                    if (type.Length > 0)
                        builder.Append("type:").Append(Quote).Append(type.ToUpperInvariant()).Append(Quote).Append('}');
                }

                if (function.TryGetProperty("response", out var response) && response.ValueKind == JsonValueKind.Object)
                {
                    builder.Append(",response:{");
                    var description = GetString(response, "description");
                    if (description.Length > 0)
                        builder.Append("description:").Append(Quote).Append(description).Append(Quote).Append(',');

                    var type = GetString(response, "type").ToUpperInvariant();
                    if (type == "OBJECT")
                        builder.Append("type:").Append(Quote).Append(type).Append(Quote).Append('}');
                }
                builder.Append('}');
                return builder.ToString();
            }
        }


        /// <summary>
        /// Formats the parameter properties of a function declaration.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="isFilterKeys">if set to <c>true</c> the schema keywords are skipped.</param>
        private static string FormatParameters(JsonElement properties, bool isFilterKeys)
        {
            var builder = new StringBuilder();
            if (properties.ValueKind != JsonValueKind.Object)
                return string.Empty;

            var isFirst = true;
            foreach (var property in GetSortedProperties(properties))
            {
                if (isFilterKeys && _standardKeys.Contains(property.Name))
                    continue;

                if (!isFirst)
                    builder.Append(',');

                isFirst = false;
                builder.Append(property.Name).Append(":{");
                var parts = new List<string>();
                var value = property.Value;
                var isObject = value.ValueKind == JsonValueKind.Object;
                var type = isObject ? GetString(value, "type").ToUpperInvariant() : string.Empty;
                var description = isObject ? GetString(value, "description") : string.Empty;
                if (description.Length > 0)
                    parts.Add($"description:{Quote}{description}{Quote}");

                if (type == "STRING")
                {
                    if (value.TryGetProperty("enum", out var enumValues) && IsTruthy(enumValues))
                        parts.Add($"enum:{FormatArgument(enumValues, true)}");
                }
                else if (type == "ARRAY")
                {
                    if (value.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Object && IsTruthy(items))
                        parts.Add($"items:{{{FormatItems(items)}}}");
                }

                if (isObject && value.TryGetProperty("nullable", out var nullable) && IsTruthy(nullable))
                    parts.Add("nullable:true");

                if (type == "OBJECT")
                {
                    if (value.TryGetProperty("properties", out var nested) && nested.ValueKind == JsonValueKind.Object)
                        parts.Add($"properties:{{{FormatParameters(nested, false)}}}");
                    else
                        parts.Add($"properties:{{{FormatParameters(value, true)}}}");

                    if (value.TryGetProperty("required", out var required) && IsTruthy(required))
                        parts.Add($"required:[{FormatRequired(required)}]");
                }

                parts.Add($"type:{Quote}{type}{Quote}");
                builder.Append(string.Join(',', parts)).Append('}');
            }
            return builder.ToString();
        }


        /// <summary>
        /// Formats the items schema of an array parameter.
        /// </summary>
        /// <param name="items">The items.</param>
        private static string FormatItems(JsonElement items)
        {
            var parts = new List<string>();
            foreach (var item in GetSortedProperties(items))
            {
                if (item.Value.ValueKind == JsonValueKind.Null)
                    continue;

                if (item.Name == "properties")
                    parts.Add($"properties:{{{FormatParameters(item.Value, false)}}}");
                else if (item.Name == "required")
                    parts.Add($"required:[{FormatRequired(item.Value)}]");
                else if (item.Name == "type" && item.Value.ValueKind == JsonValueKind.String)
                    parts.Add($"type:{Quote}{item.Value.GetString().ToUpperInvariant()}{Quote}");
                else if (item.Name == "type" && item.Value.ValueKind == JsonValueKind.Array)
                    parts.Add($"type:[{string.Join(',', item.Value.EnumerateArray().Select(x => $"{Quote}{ToText(x, false).ToUpperInvariant()}{Quote}"))}]");
                else
                    parts.Add($"{item.Name}:{FormatArgument(item.Value, true)}");
            }
            return string.Join(',', parts);
        }


        /// <summary>
        /// Formats the required parameter names.
        /// </summary>
        /// <param name="required">The required names.</param>
        private static string FormatRequired(JsonElement required)
        {
            if (required.ValueKind != JsonValueKind.Array)
                return string.Empty;

            return string.Join(',', required.EnumerateArray().Select(x => $"{Quote}{ToText(x, false)}{Quote}"));
        }


        /// <summary>
        /// Formats a value in the Gemma argument notation, strings are wrapped in the quote token.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="isEscapeKeys">if set to <c>true</c> object keys are wrapped in the quote token.</param>
        private static string FormatArgument(JsonElement argument, bool isEscapeKeys)
        {
            switch (argument.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return "null";
                case JsonValueKind.String:
                    return $"{Quote}{argument.GetString()}{Quote}";
                case JsonValueKind.True:
                    return "true";
                case JsonValueKind.False:
                    return "false";
                case JsonValueKind.Object:
                    var properties = GetSortedProperties(argument).Select(x => $"{(isEscapeKeys ? $"{Quote}{x.Name}{Quote}" : x.Name)}:{FormatArgument(x.Value, isEscapeKeys)}");
                    return $"{{{string.Join(',', properties)}}}";
                case JsonValueKind.Array:
                    return $"[{string.Join(',', argument.EnumerateArray().Select(x => FormatArgument(x, isEscapeKeys)))}]";
                default:
                    return argument.GetRawText();
            }
        }


        /// <summary>
        /// Gets a string property, empty if it is missing or not a string.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="name">The name.</param>
        private static string GetString(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();

            return string.Empty;
        }


        /// <summary>
        /// Determines whether the element is truthy the way a template tests it (not null, false, empty or zero).
        /// </summary>
        /// <param name="element">The element.</param>
        private static bool IsTruthy(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => element.EnumerateObject().Any(),
                JsonValueKind.Array => element.GetArrayLength() > 0,
                JsonValueKind.String => element.GetString().Length > 0,
                JsonValueKind.Number => element.GetRawText() != "0",
                JsonValueKind.True => true,
                _ => false
            };
        }


        /// <summary>
        /// The kind of block the prompt last wrote, this decides how the generation prompt starts.
        /// </summary>
        private enum MessageType
        {
            None = 0,
            Think = 1,
            Tool = 2,
            ToolCall = 3,
            ToolResponse = 4
        }
    }
}
