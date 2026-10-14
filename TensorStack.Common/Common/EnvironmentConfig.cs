using System.Collections.Generic;

namespace TensorStack.Common
{
    public record EnvironmentConfig
    {
        public string Name { get; init; }
        public string Version { get; set; }
        public string Directory { get; init; }
        public string[] Requirements { get; init; }
        public Dictionary<string, string> Variables { get; set; }
        public bool IsDebug { get; set; }
    }
}
