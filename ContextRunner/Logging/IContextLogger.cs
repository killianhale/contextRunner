using System;
using System.Collections.Concurrent;
using ContextRunner.Base;
using Microsoft.Extensions.Logging;

namespace ContextRunner.Logging
{
    public interface IContextLogger
    {
        ConcurrentQueue<ContextLogEntry> LogEntries { get; }
        Exception ErrorToEmit { get; set; }
        IObservable<ContextLogEntry> WhenEntryLogged { get; }
        bool TrySetContext(IActionContext context);
        void CompleteIfRoot();
        void Log(LogLevel level, string message, bool outputOnlyWithError = false);
        void Debug(string message, bool outputOnlyWithError = false);
        void Error(string message, bool outputOnlyWithError = false);
        void Critical(string message, bool outputOnlyWithError = false);
        void Information(string message, bool outputOnlyWithError = false);
        void Trace(string message, bool outputOnlyWithError = false);
        void Warning(string message, bool outputOnlyWithError = false);
        LogLevel GetHighestLogLevel();
        ContextLogEntry GetSummaryLogEntry();
    }
}