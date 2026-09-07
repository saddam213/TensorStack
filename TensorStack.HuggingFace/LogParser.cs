using System;
using System.Collections.Generic;
using System.Globalization;

namespace TensorStack.HuggingFace
{

    internal record LogEntry(DateTime Timestamp, string Message);

    internal static class LogParser
    {
        /// <summary>
        /// Parses the logs.
        /// </summary>
        /// <param name="logEntries">The log entries.</param>
        /// <returns>IEnumerable&lt;PipelineProgress&gt;.</returns>
        internal static IEnumerable<LogEntry> ParseLogs(IReadOnlyList<string> logEntries)
        {
            foreach (var logEntry in logEntries)
            {
                var progress = ParseLog(logEntry);
                if (progress == null)
                    continue;

                yield return progress;
            }
        }


        /// <summary>
        /// Parses the log.
        /// </summary>
        /// <param name="logEntry">The log entry.</param>
        /// <returns>LogEntry.</returns>
        private static LogEntry ParseLog(string logEntry)
        {
            try
            {
                var messageSections = logEntry.Split('|', 2, StringSplitOptions.TrimEntries).AsSpan();
                if (messageSections.Length < 2)
                    return default;

                var message = messageSections[1].Trim([' ', '\n', '\r', '\t']);
                if (string.IsNullOrWhiteSpace(message) || message.Length < 5 || !message.StartsWith('['))
                    return default;

                return new LogEntry(DateTime.Parse(messageSections[0], CultureInfo.InvariantCulture), message);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
