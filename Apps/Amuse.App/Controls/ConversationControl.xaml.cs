
using Amuse.App.Common;
using Amuse.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TensorStack.Common;

namespace Amuse.App.Controls
{
    /// <summary>
    /// Interaction logic for ConversationControl.xaml
    /// </summary>
    public partial class ConversationControl : TokenStreamBaseControl
    {
        ///// <summary>
        /// Initializes a new instance of the <see cref="ConversationControl"/> class.
        /// </summary>
        public ConversationControl()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Clears thes control
        /// </summary>
        public override async Task ClearAsync()
        {
            TokenCount = 0;
            Progress.Clear();
            await ClearConversationAsync();
        }


        /// <summary>
        /// Load conversation
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        public async Task LoadConversationAsync(IEnumerable<ConversationModel> conversation)
        {
            if (conversation.IsNullOrEmpty())
                return;

            var markdownConversation = new StringBuilder();
            foreach (var message in conversation)
            {
                var markdown = GenerateMarkdown(message);
                markdownConversation.AppendLine(markdown);
            }
            await ResultControl.SetTextAsync(markdownConversation.ToString());
        }


        /// <summary>
        /// Reload conversation
        /// </summary>
        public async Task ReloadConversationAsync()
        {
            await LoadConversationAsync(Conversation);
        }


        /// <summary>
        /// Clears the conversation
        /// </summary>
        public override async Task ClearConversationAsync()
        {
            await base.ClearConversationAsync();
            await ResultControl.ClearAsync();
        }


        /// <summary>
        /// Adds the system prompt asynchronous.
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        public override async Task AddSystemPromptAsync(string prompt)
        {
            ResetCurrentResult();

            var existingSystemPrompt = Conversation.FirstOrDefault(x => x.Role == ConversationRole.System);
            if (existingSystemPrompt == null)
            {
                if (string.IsNullOrEmpty(prompt))
                    return;

                AddConversationMessage(ConversationRole.System, prompt);
                await ReloadConversationAsync();
            }
            else
            {
                if (string.IsNullOrEmpty(prompt))
                {
                    Conversation.Remove(existingSystemPrompt);
                    await ReloadConversationAsync();
                }
                else if (existingSystemPrompt.Content != prompt)
                {
                    existingSystemPrompt.Content = prompt;
                    await ReloadConversationAsync();
                }
            }
        }


        /// <summary>
        /// Adds the user prompt
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="audioIndex">Index of the audio.</param>
        public override async Task AddUserPromptAsync(string prompt, List<int> imageIndex = default, List<int> audioIndex = default)
        {
            ResetCurrentResult();

            var message = AddConversationMessage(ConversationRole.User, prompt, imageIndex, audioIndex);
            var markdown = GenerateMarkdown(message, ConversationRole.Assistant);
            await ResultControl.AppendTextAsync(markdown);
        }


        /// <summary>
        /// Adds the assistant response
        /// </summary>
        /// <param name="response">The response.</param>
        public override async Task AddAssistantResponseAsync(string response)
        {
            await base.AddAssistantResponseAsync(response);
            var markdown = GenerateMarkdown(CurrentResult);
            await ResultControl.AppendTextAsync(markdown + '\n');
        }


        /// <summary>
        /// End stream response
        /// </summary>
        public override async Task<string> EndStreamResponseAsync()
        {
            var response = await base.EndStreamResponseAsync();
            if (response == null)
                return response;

            var isUnclosed = RegexManager.HasUnclosedFence(response);
            var markdownClose = AssistantCloseTag(isUnclosed);
            await ResultControl.AppendTextAsync(markdownClose);
            return response;
        }


        /// <summary>
        /// Called when stream flush complete
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task OnStreamFlushAsync(string buffer)
        {
            await base.OnStreamFlushAsync(buffer);
            await ResultControl.AppendStreamAsync(buffer);
        }


        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseEnter" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (!IsKeyboardFocusWithin)
                Focus();
        }


        private static string AssistantCloseTag(bool isUnclosed)
        {
            const string assistantClosed = $"\n</assistant>\n";
            const string assistantUnclosed = $"\n```\n</assistant>\n";
            return isUnclosed ? assistantUnclosed : assistantClosed;
        }


        private static string GenerateMarkdown(ConversationModel message, ConversationRole? nextRole = null)
        {
            const string user = "<user>\n{0}\n</user>{1}";
            const string system = "<system>\n{0}\n</system>{1}";
            const string assistant = "<assistant>\n{0}\n</assistant>{1}";
            var nextRoleTag = nextRole.HasValue ? $"\n{GenerateTag(nextRole.Value, true)}\n" : string.Empty;

            if (message.Role == ConversationRole.User && !message.ImageIndex.IsNullOrEmpty())
            {
                var userMessage = new StringBuilder();
                foreach (var imageIndex in message.ImageIndex)
                {
                    userMessage.Append($"![](https://history/GenerateImage_uvy3qytd.png)");
                }
                userMessage.AppendLine();
                userMessage.AppendLine(message.Content);
                return string.Format(user, userMessage, nextRoleTag);
            }

            return message.Role switch
            {
                ConversationRole.User => string.Format(user, message.Content, nextRoleTag),
                ConversationRole.System => string.Format(system, message.Content, nextRoleTag),
                ConversationRole.Assistant => string.Format(assistant, message.Content, nextRoleTag),
                _ => throw new NotImplementedException()
            };
        }


        private static string GenerateTag(ConversationRole role, bool isOpen)
        {
            return role switch
            {
                ConversationRole.System => isOpen ? "<system>" : "</system>",
                ConversationRole.User => isOpen ? "<user>" : "</user>",
                ConversationRole.Assistant => isOpen ? "<assistant>" : "</assistant>",
                _ => throw new NotImplementedException()
            };
        }
    }
}
