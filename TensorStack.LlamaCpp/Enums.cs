namespace TensorStack.LlamaCpp
{
    public enum LogLevelType
    {
        None = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Continue = 5
    }


    public enum PromptTemplate
    {
        Qwen30 = 0,
        Qwen35 = 1,
        Qwen36 = 2,
        Qwen38 = 3,
        Gemma4 = 10
    }


    public enum CacheDataType
    {
        F16 = 0,
        Q8_0 = 1,
        Q4_0 = 2
    }


    public enum ConversationRole
    {
        User = 0,
        System = 1,
        Assistant = 2,
        Tool = 3
    }
}
