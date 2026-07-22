using System.Text.Json.Serialization;

namespace TensorStack.Python.Common
{
    public record ConversationMessage(ConversationRole Role, string Content, int[] ImageIndex);

    public enum ConversationRole
    {
        [JsonStringEnumMemberName("user")]
        User = 0,

        [JsonStringEnumMemberName("system")]
        System = 1,

        [JsonStringEnumMemberName("assistant")]
        Assistant = 2
    }
}
