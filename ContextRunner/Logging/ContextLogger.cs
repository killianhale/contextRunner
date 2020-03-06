using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using ContextRunner.Base;

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
    }
}
