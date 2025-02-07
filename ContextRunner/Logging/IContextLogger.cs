using ContextRunner.Base;

namespace ContextRunner.Logging
{
    public interface IContextLogger
    {
        ConcurrentQueue<ContextLogEntry> LogEntries { get; }
        Exception? ErrorToEmit { get; set; }
        IObservable<ContextLogEntry> WhenEntryLogged { get; }
        bool TrySetContext(IActionContext context);
        void CompleteIfRoot();
        void LogAsType(LogLevel level, string message,
            ContextLogEntryType entryType = ContextLogEntryType.AlwaysShow);
        void Log(LogLevel level, string message, bool showOnlyOnError = false);
        void Debug(string message, bool showOnlyOnError = false);
        void Error(string message, bool showOnlyOnError = false);
        void Critical(string message, bool showOnlyOnError = false);
        void Information(string message, bool showOnlyOnError = false);
        void Trace(string message, bool showOnlyOnError = false);
        void Warning(string message, bool showOnlyOnError = false);
        LogLevel GetHighestLogLevel();
        ContextLogEntry GetSummaryLogEntry();
    }
}