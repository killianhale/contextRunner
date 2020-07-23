using System;
using Microsoft.Extensions.Logging;

namespace ContextRunner.Logging
{
    public class ContextLogEntry
    {
        public ContextLogEntry(
            int contextDepth,
            string contextName,
            string message,
            LogLevel logLevel,
            TimeSpan timeElapsed,
            bool outputOnlyWithError = false)
        {
            ContextDepth = contextDepth;
            ContextName = contextName;
            Message = message;
            LogLevel = logLevel;
            TimeElapsed = timeElapsed;
            OutputOnlyWithError = outputOnlyWithError;
        }

        public bool OutputOnlyWithError { get; }
        public int ContextDepth { get; }
        public string ContextName { get; }
        public string Message { get; }
        public LogLevel LogLevel { get; }
        public TimeSpan TimeElapsed { get; }
    }
}
