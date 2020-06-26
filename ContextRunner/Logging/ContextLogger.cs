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
            if (_context.IsRoot)
            {
                _logSubject.OnCompleted();
            }
        }

        public void CompleteWithError(Exception ex)
        {
            _logSubject.OnError(ex);
        }

        public void Log(LogLevel level, string message)
        {
            var entry = new ContextLogEntry(
                _context.Depth,
                _context.ContextName,
                message,
                level,
                _context.TimeElapsed
            );

            _logSubject.OnNext(entry);

            LogEntries.Enqueue(entry);
        }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void Critical(string message)
        {
            Log(LogLevel.Critical, message);
        }

        public void Information(string message)
        {
            Log(LogLevel.Information, message);
        }

        public void Trace(string message)
        {
            Log(LogLevel.Trace, message);
        }

        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
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
            var message = $"The context '{_context.ContextName}' ended without error.";

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
