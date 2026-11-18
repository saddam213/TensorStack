namespace TensorStack.LlamaCpp.Common
{
    public record ConversationMessage(ConversationRole Role, string Content, int[] ImageIndex = null, int[] AudioIndex = null, string[] ToolCalls = null);
}
