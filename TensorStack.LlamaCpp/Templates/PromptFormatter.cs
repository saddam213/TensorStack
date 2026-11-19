using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using TensorStack.LlamaCpp.Common;

namespace TensorStack.LlamaCpp.Templates
{
    /// <summary>
    /// Formats a conversation into the prompt a model family was trained with (the C# equivalent of the model chat template).
    /// </summary>
    internal abstract class PromptFormatter
    {
        private readonly string _mediaMarker;

        /// <summary>
        /// Initializes a new instance of the <see cref="PromptFormatter"/> class.
        /// </summary>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        protected PromptFormatter(string mediaMarker)
        {
            _mediaMarker = mediaMarker;
        }

        /// <summary>
        /// Gets the marker that is replaced by an input image or audio.
        /// </summary>
        protected string MediaMarker => _mediaMarker;

        /// <summary>
        /// Gets the tokens that end the generation in addition to the end tokens the model declares.
        /// </summary>
        public abstract IReadOnlyList<string> StopTokens { get; }

        /// <summary>
        /// Gets the tokens of the prompt format, these are removed from message content so content can not alter the prompt structure.
        /// </summary>
        public abstract IReadOnlyList<string> ReservedTokens { get; }


        /// <summary>
        /// Formats the conversation.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <param name="tools">The tools, each an OpenAI style function JSON schema.</param>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        public abstract PromptResult Format(IReadOnlyList<ConversationMessage> messages, IReadOnlyList<string> tools, bool isThinkingEnabled);


        /// <summary>
        /// Creates the formatter for the template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        public static PromptFormatter Create(PromptTemplate template, string mediaMarker)
        {
            return template switch
            {
                PromptTemplate.Qwen30 => new Qwen30Formatter(mediaMarker),
                PromptTemplate.Qwen35 => new Qwen35Formatter(mediaMarker),
                PromptTemplate.Qwen36 => new Qwen36Formatter(mediaMarker),
                PromptTemplate.Qwen38 => new Qwen38Formatter(mediaMarker),
                PromptTemplate.Gemma4 => new Gemma4Formatter(mediaMarker),
                _ => throw new NotSupportedException($"Prompt template '{template}' is not supported.")
            };
        }


        /// <summary>
        /// Appends the media markers of the message images and records them in prompt order.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="message">The message.</param>
        /// <param name="media">The media.</param>
        protected void AppendImages(StringBuilder builder, ConversationMessage message, List<PromptMedia> media)
        {
            if (message.ImageIndex == null)
                return;

            foreach (var index in message.ImageIndex)
            {
                builder.Append(_mediaMarker);
                media.Add(new PromptMedia(false, index));
            }
        }


        /// <summary>
        /// Appends the media markers of the message audios and records them in prompt order.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="message">The message.</param>
        /// <param name="media">The media.</param>
        protected void AppendAudios(StringBuilder builder, ConversationMessage message, List<PromptMedia> media)
        {
            if (message.AudioIndex == null)
                return;

            foreach (var index in message.AudioIndex)
            {
                builder.Append(_mediaMarker);
                media.Add(new PromptMedia(true, index));
            }
        }


        /// <summary>
        /// Parses the tool calls of an assistant message, each a JSON object with name and arguments.
        /// </summary>
        /// <param name="message">The message.</param>
        protected static List<PromptToolCall> ParseToolCalls(ConversationMessage message)
        {
            var toolCalls = new List<PromptToolCall>();
            if (message.ToolCalls == null)
                return toolCalls;

            foreach (var toolCall in message.ToolCalls)
            {
                if (string.IsNullOrWhiteSpace(toolCall))
                    continue;

                using (var document = JsonDocument.Parse(toolCall))
                {
                    // Accept both {"name","arguments"} and {"type":"function","function":{"name","arguments"}}
                    var function = document.RootElement;
                    if (function.ValueKind == JsonValueKind.Object && function.TryGetProperty("function", out var inner) && inner.ValueKind == JsonValueKind.Object)
                        function = inner;

                    var name = function.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : string.Empty;
                    var hasArguments = function.TryGetProperty("arguments", out var arguments);
                    toolCalls.Add(new PromptToolCall(name, hasArguments ? arguments.Clone() : default, hasArguments));
                }
            }
            return toolCalls;
        }


        /// <summary>
        /// Splits assistant content into its reasoning and response, the reasoning is the text inside the think tags.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="reasoning">The reasoning.</param>
        /// <returns>The response without the reasoning.</returns>
        protected static string SplitReasoning(string content, out string reasoning)
        {
            const string thinkStart = "<think>";
            const string thinkEnd = "</think>";
            reasoning = string.Empty;
            var endIndex = content.LastIndexOf(thinkEnd, StringComparison.Ordinal);
            if (endIndex < 0)
                return content;

            var firstEndIndex = content.IndexOf(thinkEnd, StringComparison.Ordinal);
            reasoning = content.Substring(0, firstEndIndex).TrimEnd('\n');
            var startIndex = reasoning.LastIndexOf(thinkStart, StringComparison.Ordinal);
            if (startIndex >= 0)
                reasoning = reasoning.Substring(startIndex + thinkStart.Length);

            reasoning = reasoning.TrimStart('\n');
            return content.Substring(endIndex + thinkEnd.Length).TrimStart('\n');
        }


        /// <summary>
        /// Serializes JSON the way the Python json module does (", " and ": " separators), chat templates were trained with this layout.
        /// </summary>
        /// <param name="element">The element.</param>
        protected static string ToJson(JsonElement element)
        {
            var builder = new StringBuilder();
            WriteJson(builder, element);
            return builder.ToString();
        }


        /// <summary>
        /// Writes the JSON element.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="element">The element.</param>
        private static void WriteJson(StringBuilder builder, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    builder.Append('{');
                    var isFirstProperty = true;
                    foreach (var property in element.EnumerateObject())
                    {
                        if (!isFirstProperty)
                            builder.Append(", ");

                        isFirstProperty = false;
                        WriteJsonString(builder, property.Name);
                        builder.Append(": ");
                        WriteJson(builder, property.Value);
                    }
                    builder.Append('}');
                    break;
                case JsonValueKind.Array:
                    builder.Append('[');
                    var isFirstItem = true;
                    foreach (var item in element.EnumerateArray())
                    {
                        if (!isFirstItem)
                            builder.Append(", ");

                        isFirstItem = false;
                        WriteJson(builder, item);
                    }
                    builder.Append(']');
                    break;
                case JsonValueKind.String:
                    WriteJsonString(builder, element.GetString());
                    break;
                case JsonValueKind.True:
                    builder.Append("true");
                    break;
                case JsonValueKind.False:
                    builder.Append("false");
                    break;
                case JsonValueKind.Number:
                    builder.Append(element.GetRawText());
                    break;
                default:
                    builder.Append("null");
                    break;
            }
        }


        /// <summary>
        /// Writes the JSON string, non ASCII characters are kept as they are.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="value">The value.</param>
        private static void WriteJsonString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (var character in value)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    default:
                        if (character < ' ')
                            builder.Append("\\u").Append(((int)character).ToString("x4"));
                        else
                            builder.Append(character);
                        break;
                }
            }
            builder.Append('"');
        }


        /// <summary>
        /// Gets the object properties sorted by name ignoring case (the template dictsort filter).
        /// </summary>
        /// <param name="element">The element.</param>
        protected static IEnumerable<JsonProperty> GetSortedProperties(JsonElement element)
        {
            return element.EnumerateObject().OrderBy(x => x.Name.ToLowerInvariant(), StringComparer.Ordinal);
        }


        /// <summary>
        /// Gets the text of a scalar value the way a template prints it.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="isPythonLiteral">if set to <c>true</c> booleans and null print as Python literals (True, False, None).</param>
        protected static string ToText(JsonElement element, bool isPythonLiteral)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => isPythonLiteral ? "True" : "true",
                JsonValueKind.False => isPythonLiteral ? "False" : "false",
                JsonValueKind.Null or JsonValueKind.Undefined => isPythonLiteral ? "None" : "null",
                JsonValueKind.Number => element.GetRawText(),
                _ => ToJson(element)
            };
        }
    }


    /// <summary>
    /// The formatted prompt and the media it references in prompt order.
    /// </summary>
    /// <param name="Prompt">The prompt, each media is represented by the media marker.</param>
    /// <param name="Media">The media in the order the markers appear in the prompt.</param>
    internal sealed record PromptResult(string Prompt, IReadOnlyList<PromptMedia> Media);


    /// <summary>
    /// A media reference of the prompt.
    /// </summary>
    /// <param name="IsAudio">if set to <c>true</c> the index refers to the input audios, otherwise the input images.</param>
    /// <param name="Index">The input index.</param>
    internal sealed record PromptMedia(bool IsAudio, int Index);


    /// <summary>
    /// A tool call made by the assistant.
    /// </summary>
    /// <param name="Name">The function name.</param>
    /// <param name="Arguments">The arguments.</param>
    /// <param name="HasArguments">if set to <c>true</c> the call has arguments.</param>
    internal sealed record PromptToolCall(string Name, JsonElement Arguments, bool HasArguments);
}
