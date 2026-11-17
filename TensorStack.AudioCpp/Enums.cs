namespace TensorStack.AudioCpp
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


    /// <summary>
    /// The weight storage type, Default keeps the type the model was saved with.
    /// </summary>
    public enum DataType
    {
        Default = 0,
        F32 = 1,
        F16 = 2,
        BF16 = 3,
        Q8_0 = 4,
        Q4_0 = 5,
        Q4_K = 6
    }
}
