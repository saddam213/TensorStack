using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TensorStack.LlamaCpp.Common;

namespace TensorStack.LlamaCpp.Templates
{
    /// <summary>
    /// Base prompt formatter for the Qwen 3.x family (ChatML), the version specific parts of the chat templates are virtual members.
    /// </summary>
    internal abstract class QwenFormatter : PromptFormatter
    {
        protected const string ToolResponseStart = "<tool_response>";
        protected const string ToolResponseEnd = "</tool_response>";
        private static readonly string[] _stopTokens = ["<|im_end|>", "<|endoftext|>"];
        private static readonly string[] _reservedTokens = ["<|im_start|>", "<|im_end|>", "<|endoftext|>", "<|vision_start|>", "<|vision_end|>", "<|image_pad|>", "<|video_pad|>"];

        /// <summary>
        /// Initializes a new instance of the <see cref="QwenFormatter"/> class.
        /// </summary>
        /// <param name="mediaMarker">The marker that is replaced by an input image or audio.</param>
        protected QwenFormatter(string mediaMarker)
            : base(mediaMarker)
        {
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
        /// Gets a value indicating whether a tool response at the start of the conversation opens a user turn.
        /// </summary>
        protected virtual bool OpensUserTurnForLeadingToolResponse => false;


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
            var hasSystem = messages.Count > 0 && messages[0].Role == ConversationRole.System;
            var systemContent = hasSystem ? GetText(messages[0]) : string.Empty;

            // System and Tools
            if (tools != null && tools.Count > 0)
                AppendToolSystem(builder, tools, hasSystem, systemContent, isThinkingEnabled);
            else
                AppendSystem(builder, hasSystem, systemContent, isThinkingEnabled);

            // Messages
            var lastQueryIndex = GetLastQueryIndex(messages);
            for (int i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                var isLast = i == messages.Count - 1;
                switch (message.Role)
                {
                    case ConversationRole.System:
                        if (i > 0)
                            AppendLaterSystem(builder, message);
                        break;
                    case ConversationRole.User:
                        builder.Append("<|im_start|>user\n").Append(GetContent(message, media)).Append("<|im_end|>\n");
                        break;
                    case ConversationRole.Assistant:
                        AppendAssistant(builder, message, i > lastQueryIndex, isLast);
                        break;
                    case ConversationRole.Tool:
                        AppendToolResponse(builder, messages, i);
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
        /// Appends the system block when tools are provided.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tools">The tools.</param>
        /// <param name="hasSystem">if set to <c>true</c> the first message is a system message.</param>
        /// <param name="systemContent">The system content.</param>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        protected abstract void AppendToolSystem(StringBuilder builder, IReadOnlyList<string> tools, bool hasSystem, string systemContent, bool isThinkingEnabled);


        /// <summary>
        /// Appends the system block when no tools are provided.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="hasSystem">if set to <c>true</c> the first message is a system message.</param>
        /// <param name="systemContent">The system content.</param>
        /// <param name="isThinkingEnabled">if set to <c>true</c> the model should think before it answers.</param>
        protected virtual void AppendSystem(StringBuilder builder, bool hasSystem, string systemContent, bool isThinkingEnabled)
        {
            if (hasSystem)
                builder.Append("<|im_start|>system\n").Append(systemContent).Append("<|im_end|>\n");
        }


        /// <summary>
        /// Appends a system message that is not the first message, these are dropped by default.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="message">The message.</param>
        protected virtual void AppendLaterSystem(StringBuilder builder, ConversationMessage message)
        {
        }


        /// <summary>
        /// Appends the assistant reasoning and content.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="content">The content without the reasoning.</param>
        /// <param name="reasoning">The reasoning.</param>
        /// <param name="isAfterLastQuery">if set to <c>true</c> the message follows the last user query.</param>
        /// <param name="isLast">if set to <c>true</c> this is the last message.</param>
        protected abstract void AppendAssistantContent(StringBuilder builder, string content, string reasoning, bool isAfterLastQuery, bool isLast);


        /// <summary>
        /// Appends an assistant tool call.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="toolCall">The tool call.</param>
        /// <param name="isFirst">if set to <c>true</c> this is the first tool call of the message.</param>
        /// <param name="content">The message content without the reasoning.</param>
        protected abstract void AppendToolCall(StringBuilder builder, PromptToolCall toolCall, bool isFirst, string content);


        /// <summary>
        /// Normalizes message content, Qwen 3.5+ trims the content.
        /// </summary>
        /// <param name="content">The content.</param>
        protected virtual string NormalizeContent(string content)
        {
            return content.Trim();
        }


        /// <summary>
        /// Appends the tool schemas, one JSON object per line.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tools">The tools.</param>
        protected static void AppendTools(StringBuilder builder, IReadOnlyList<string> tools)
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
        /// Gets the message text.
        /// </summary>
        /// <param name="message">The message.</param>
        protected string GetText(ConversationMessage message)
        {
            return NormalizeContent(message.Content ?? string.Empty);
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
            // Content
            var content = SplitReasoning(GetText(message), out var reasoning);
            builder.Append("<|im_start|>assistant\n");
            AppendAssistantContent(builder, content, reasoning, isAfterLastQuery, isLast);

            // Tool Calls
            var toolCalls = ParseToolCalls(message);
            for (int i = 0; i < toolCalls.Count; i++)
            {
                AppendToolCall(builder, toolCalls[i], i == 0, content);
            }
            builder.Append("<|im_end|>\n");
        }


        /// <summary>
        /// Appends a tool response, consecutive tool responses share one user turn.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="messages">The messages.</param>
        /// <param name="index">The index.</param>
        private void AppendToolResponse(StringBuilder builder, IReadOnlyList<ConversationMessage> messages, int index)
        {
            var isFirst = index == 0;
            var isLast = index == messages.Count - 1;
            var isPreviousTool = !isFirst && messages[index - 1].Role == ConversationRole.Tool;
            var isNextTool = !isLast && messages[index + 1].Role == ConversationRole.Tool;

            if (isFirst ? OpensUserTurnForLeadingToolResponse : !isPreviousTool)
                builder.Append("<|im_start|>user");

            builder.Append('\n').Append(ToolResponseStart).Append('\n').Append(GetText(messages[index])).Append('\n').Append(ToolResponseEnd);
            if (isLast || !isNextTool)
                builder.Append("<|im_end|>\n");
        }


        /// <summary>
        /// Gets the index of the last user query, user messages that only carry tool responses are not queries.
        /// </summary>
        /// <param name="messages">The messages.</param>
        private int GetLastQueryIndex(IReadOnlyList<ConversationMessage> messages)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].Role != ConversationRole.User)
                    continue;

                var content = GetText(messages[i]);
                if (!(content.StartsWith(ToolResponseStart, StringComparison.Ordinal) && content.EndsWith(ToolResponseEnd, StringComparison.Ordinal)))
                    return i;
            }
            return messages.Count - 1;
        }


        /// <summary>
        /// Gets the message content with its media, images precede the text and audios follow it.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="media">The media.</param>
        private string GetContent(ConversationMessage message, List<PromptMedia> media)
        {
            var builder = new StringBuilder();
            AppendImages(builder, message, media);
            builder.Append(message.Content ?? string.Empty);
            AppendAudios(builder, message, media);
            return NormalizeContent(builder.ToString());
        }
    }
}
