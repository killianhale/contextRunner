namespace ContextRunner.Logging
{
    public class ContextLogEntry
    {
        public ContextLogEntry(
            int contextDepth,
            string? contextName,
            Guid contextId,
            string message,
            LogLevel logLevel,
            TimeSpan timeElapsed,
            DateTime timestamp,
            ContextLogEntryType entryType = ContextLogEntryType.AlwaysShow)
        {
            ContextDepth = contextDepth;
            ContextName = contextName;
            ContextId = contextId;
            Message = message;
            LogLevel = logLevel;
            TimeElapsed = timeElapsed;
            Timestamp = timestamp;
            EntryType = entryType;
        }

        public ContextLogEntryType EntryType { get; }
        public int ContextDepth { get; }
        public string? ContextName { get; }
        public Guid ContextId { get; }
        public string Message { get; }
        public LogLevel LogLevel { get; }
        public TimeSpan TimeElapsed { get; }
        public DateTime Timestamp { get; }
    }
}
