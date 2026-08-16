using System.Collections.Generic;


namespace Amuse.Common
{
    public sealed record PipelineCreateOptions
    {
        public bool IsDebug { get; set; }
        public string Directory { get; set; }
        public string Environment { get; set; }
        public string HostVersion { get; set; }
        public string[] Requirements { get; set; }
        public Dictionary<string, string> Variables { get; set; }
        public EnvironmentMode Mode { get; set; }
        public short ServerPort { get; set; }
        public string ServerAddress { get; set; }
    }
}
