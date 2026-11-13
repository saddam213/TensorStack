namespace TensorStack.WPF
{
    public interface IUIConfiguration
    {
        string DirectoryTemp { get; }
        double VolumeInput { get; set; }
        double VolumeOutput { get; set; }
        bool IsVolumeInputMute { get; set; }
        bool IsVolumeOutputMute { get; set; }
        double UIScale { get; set; }
        bool IsExternalLinksEnabled { get; set; }
        bool IsExternalLinksAcknowledged { get; set; }
        bool IsExternalToolsEnabled { get; set; }
        bool IsExternalToolsAcknowledged { get; set; }
        string UserAgent { get; set; }
    }


    public record DefaultUIConfiguration : IUIConfiguration
    {
        public string DirectoryTemp { get; init; }
        public double VolumeInput { get; set; } = 0.1;
        public double VolumeOutput { get; set; } = 0.1;
        public bool IsVolumeInputMute { get; set; }
        public bool IsVolumeOutputMute { get; set; }
        public double UIScale { get; set; } = 1;
        public bool IsExternalLinksEnabled { get; set; }
        public bool IsExternalLinksAcknowledged { get; set; }
        public bool IsExternalToolsEnabled { get; set; }
        public bool IsExternalToolsAcknowledged { get; set; }
        public string UserAgent { get; set; }
    }
}