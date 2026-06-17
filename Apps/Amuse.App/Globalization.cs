using Amuse.Common;
using System.Collections.Generic;

namespace Amuse.App
{
    public static class Globalization
    {
        public static string GetProgressMessage(PipelineProgress progress)
        {
            if (!string.IsNullOrEmpty(progress.Subkey) && ProgressMessages.TryGetValue(progress.Subkey, out string messageTempate))
            {
                return string.Format(
                    messageTempate,
                    progress.Key,
                    progress.Subkey,
                    progress.ElapsedKey,
                    progress.Timestamp,
                    progress.Elapsed,
                    progress.Value,
                    progress.Maximum,
                    progress.BatchValue,
                    progress.BatchMaximum,
                    progress.Message
                );
            }
            return progress.Message ?? progress.Subkey ?? progress.Key;
        }


        private readonly static Dictionary<string, string> ProgressMessages = new Dictionary<string, string>()
        {
            {"Initialize",  "Initializing Pipeline..." },
            {"TextEncoder", "Encoding Prompt..." },
            {"Transformer", "Generating..." },
            {"Step",        "Step: {5}/{6}" },
            {"AutoEncoder", "Generating Result..." },
            {"Complete",    "Generating Result..." }
        };
    }
}
