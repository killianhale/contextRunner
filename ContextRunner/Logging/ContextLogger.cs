using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using ContextRunner.Base;
using System.Collections.Generic;
using System.Linq;

namespace ContextRunner.Logging
{
    public class ContextLogger : IContextLogger
    {
        private IActionContext _context;
        private readonly Subject<ContextLogEntry> _logSubject;

        public ContextLogger(IActionContext context)
        {
            _context = context;
            _logSubject = new Subject<ContextLogEntry>();

            LogEntries = new ConcurrentQueue<ContextLogEntry>();
        }

        public ConcurrentQueue<ContextLogEntry> LogEntries { get; }

        public Exception ErrorToEmit { get; set; }

        public IObservable<ContextLogEntry> WhenEntryLogged
        {
            get => _logSubject;
        }

        public bool TrySetContext(IActionContext context)
        {
            if(context == null)
            {
                return false;
            }

            _context = context;

            return true;
        }

        public void CompleteIfRoot()
        {
            if (!_context.IsRoot)
            {
                return;
            }

            if(ErrorToEmit != null)
            {
                CompleteWithError();

                return;
            }

            RemoveErrorOnlyEntries(false);

            _logSubject.OnCompleted();
        }

        private void CompleteWithError()
        {
            RemoveErrorOnlyEntries(true);

            _logSubject.OnError(ErrorToEmit);
        }

        private void RemoveErrorOnlyEntries(bool isError)
        {
            var nonErrorEntries = LogEntries.Where(entry => entry.EntryType != ContextLogEntryType.ShowOnlyOnError || (isError && entry.EntryType == ContextLogEntryType.ShowOnlyOnError))
                .ToList();
            
            var shouldShowContextEndMessages = (isError && _context.Settings.AlwaysShowContextEndMessagesOnError) || _context.Settings.EnableContextEndMessage;

            if (!shouldShowContextEndMessages)
            {
                nonErrorEntries = nonErrorEntries.Where(entry => entry.EntryType != ContextLogEntryType.ContextEnd)
                    .ToList();
            }
            
            var shouldShowContextStartMessages = (isError && _context.Settings.AlwaysShowContextStartMessagesOnError) || _context.Settings.EnableContextStartMessage;

            if (!shouldShowContextStartMessages)
            {
                nonErrorEntries = nonErrorEntries.Where(entry => entry.EntryType != ContextLogEntryType.ContextStart)
                    .ToList();
            }
            
            var shouldSuppressEndMessage = _context.Settings.SuppressChildContextEndMessages;
            var shouldShowChildContextEndMessages = _context.Settings.EnableContextEndMessage && !shouldSuppressEndMessage;

            if (isError)
            {
                shouldShowChildContextEndMessages = _context.Settings.AlwaysShowContextEndMessagesOnError
                                                    || shouldShowChildContextEndMessages;
            }

            if (!shouldShowChildContextEndMessages)
            {
                nonErrorEntries = nonErrorEntries.Where(entry => entry.EntryType != ContextLogEntryType.ChildContextEnd)
                    .ToList();
            }

            var shouldSuppressStartMessage = _context.Settings.SuppressChildContextStartMessages;
            var shouldShowChildContextStartMessages = _context.Settings.EnableContextStartMessage && !shouldSuppressStartMessage;

            if (isError)
            {
                shouldShowChildContextStartMessages = _context.Settings.AlwaysShowContextStartMessagesOnError
                                                 || shouldShowChildContextStartMessages;
            }

            if (!shouldShowChildContextStartMessages)
            {
                nonErrorEntries = nonErrorEntries.Where(entry => entry.EntryType != ContextLogEntryType.ChildContextStart)
                    .ToList();
            }

            LogEntries.Clear();

            nonErrorEntries.ForEach(entry => LogEntries.Enqueue(entry));

            var test = true;
        }

        public void LogAsType(LogLevel level, string message, ContextLogEntryType entryType = ContextLogEntryType.AlwaysShow)
        {
            var entry = new ContextLogEntry(
                _context.Depth,
                _context.ContextName,
                _context.Id,
                message,
                level,
                _context.TimeElapsed,
                DateTime.UtcNow,
                entryType
            );

            _logSubject.OnNext(entry);

            LogEntries.Enqueue(entry);
        }


        public void Log(LogLevel level, string message, bool showOnlyOnError = false)
        {
            var type = showOnlyOnError
                ? ContextLogEntryType.ShowOnlyOnError
                : ContextLogEntryType.AlwaysShow;
            
            LogAsType(level, message, type);
        }

        public void Debug(string message, bool showOnlyOnError = false)
        {
            Log(LogLevel.Debug, message, showOnlyOnError);
        }

        public void Error(string message, bool showOnlyOnError = false)
        {
            Log(LogLevel.Error, message, showOnlyOnError);
        }

        public void Critical(string message, bool showOnlyOnError = false)
        {
            Log(LogLevel.Critical, message, showOnlyOnError);
        }

        public void Information(string message, bool showOnlyOnError = false)
        {
            Log(LogLevel.Information, message, showOnlyOnError);
        }

        public void Trace(string message, bool showOnlyOnError = false)
        {
            Log(LogLevel.Trace, message, showOnlyOnError);
        }

        public void Warning(string message, bool showOnlyOnError = false)
        {
            Log(LogLevel.Warning, message, showOnlyOnError);
        }

        public LogLevel GetHighestLogLevel()
        {
            var search = new[] {
                LogLevel.Critical,
                LogLevel.Error,
                LogLevel.Warning,
                LogLevel.Information,
                LogLevel.Debug,
                LogLevel.Trace,
                LogLevel.None
            };

            IEnumerable<ContextLogEntry> highest = null;

            foreach (var level in search)
            {
                highest = LogEntries.Where(entry => entry.LogLevel == level);

                if (highest.Any())
                {
                    break;
                }
            }

            var highestLevel = highest.FirstOrDefault()?.LogLevel ?? LogLevel.Trace;

            return highestLevel;
        }

        public ContextLogEntry GetSummaryLogEntry()
        {
            var message = $"The context '{_context.ContextName}' ended successfully.";

            var highestLevel = GetHighestLogLevel();
            var highestLevelCount = LogEntries.Count(entry => entry.LogLevel == highestLevel);
            var highestFirstMessage = LogEntries.FirstOrDefault(entry => entry.LogLevel == highestLevel)?.Message;

            if (highestLevel == LogLevel.Critical)
            {
                message = highestLevelCount == 1
                    ? $"The context '{_context.ContextName}' ended with a critical error. {highestFirstMessage}"
                    : $"The context '{_context.ContextName}' ended with multiple critical errors.";
            }
            else if (highestLevel == LogLevel.Error)
            {
                message = highestLevelCount == 1
                    ? $"The context '{_context.ContextName}' ended with an error. {highestFirstMessage}"
                    : $"The context '{_context.ContextName}' ended with multiple errors.";
            }
            else if (highestLevel == LogLevel.Warning)
            {
                message = highestLevelCount == 1
                    ? $"The context '{_context.ContextName}' ended with a warning. {highestFirstMessage}"
                    : $"The context '{_context.ContextName}' ended with multiple warnings.";
            }

            var result = new ContextLogEntry(0, _context.ContextName, _context.Id, message, highestLevel, _context.TimeElapsed, DateTime.UtcNow, ContextLogEntryType.Summary);

            return result;
        }
    }
}
