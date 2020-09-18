using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using ContextRunner.Base;
using System.Collections.Generic;
using System.Linq;

namespace ContextRunner.Logging
{
    public class ContextLogger
    {
        private ActionContext _context;
        private readonly Subject<ContextLogEntry> _logSubject;

        public ContextLogger(ActionContext context)
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

        public bool TrySetContext(ActionContext context)
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

            RemoveErrorOnlyEntries();

            _logSubject.OnCompleted();
        }

        private void CompleteWithError()
        {
            if (!_context.Settings.IgnoreChildSuppressionOnError)
            {
                RemoveErrorOnlyEntries();
            }

            _logSubject.OnError(ErrorToEmit);
        }

        private void RemoveErrorOnlyEntries()
        {
            var nonErrorEntries = LogEntries.Where(entry => !entry.OutputOnlyWithError).ToList();

            LogEntries.Clear();

            nonErrorEntries.ForEach(entry => LogEntries.Enqueue(entry));
        }

        public void Log(LogLevel level, string message, bool outputOnlyWithError = false)
        {
            var entry = new ContextLogEntry(
                _context.Depth,
                _context.ContextName,
                message,
                level,
                _context.TimeElapsed,
                outputOnlyWithError
            );

            _logSubject.OnNext(entry);

            LogEntries.Enqueue(entry);
        }

        public void Debug(string message, bool outputOnlyWithError = false)
        {
            Log(LogLevel.Debug, message, outputOnlyWithError);
        }

        public void Error(string message, bool outputOnlyWithError = false)
        {
            Log(LogLevel.Error, message, outputOnlyWithError);
        }

        public void Critical(string message, bool outputOnlyWithError = false)
        {
            Log(LogLevel.Critical, message, outputOnlyWithError);
        }

        public void Information(string message, bool outputOnlyWithError = false)
        {
            Log(LogLevel.Information, message, outputOnlyWithError);
        }

        public void Trace(string message, bool outputOnlyWithError = false)
        {
            Log(LogLevel.Trace, message, outputOnlyWithError);
        }

        public void Warning(string message, bool outputOnlyWithError = false)
        {
            Log(LogLevel.Warning, message, outputOnlyWithError);
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

            if (highestLevel == LogLevel.Error || highestLevel == LogLevel.Critical)
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

            var result = new ContextLogEntry(0, _context.ContextName, message, highestLevel, _context.TimeElapsed);

            return result;
        }
    }
}
