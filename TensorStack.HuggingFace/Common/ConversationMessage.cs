using System.Text.Json.Serialization;

namespace TensorStack.HuggingFace.Common
{
    public record ConversationMessage(ConversationRole Role, string Content, int[] ImageIndex, int[] AudioIndex, string[] ToolCalls);

    public enum ConversationRole
    {
        [JsonStringEnumMemberName("user")]
        User = 0,

        [JsonStringEnumMemberName("system")]
        System = 1,

        [JsonStringEnumMemberName("assistant")]
        Assistant = 2,

        [JsonStringEnumMemberName("tool")]
        Tool = 3
    }
}
