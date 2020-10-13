using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ContextRunner.Logging;
using NLog;
using NLog.Targets;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ContextRunner.NLog.Internal
{
    internal interface IMemoryLogService
    {
        void AddTimestamp(Guid contextId);
        void RemoveTimestamp(Guid contextId);
        DateTime GetTimestamp(Guid contextId);
        void GetPendingMemoryLogs();
        void RemoveIrrelevantMemoryLogs();
        List<ContextLogEntry> GetMemoryLogsSince(DateTime timestamp);
    }

    internal class MemoryLogService : IMemoryLogService
    {
        private static readonly ConcurrentQueue<(DateTime Timestamp, ContextLogEntry LogEntry)> OutOfContextLogs = new ConcurrentQueue<(DateTime, ContextLogEntry)>();
        private static readonly ConcurrentDictionary<Guid, DateTime> ContextStartTimestamps = new ConcurrentDictionary<Guid, DateTime>();

        private readonly NlogContextRunnerConfig _config;
        private MemoryTarget _memoryLogTarget;
        private Timer _logCleanupTimer;

        public MemoryLogService(NlogContextRunnerConfig config)
        {
            _config = config;

            Setup();
        }

        private void Setup()
        {
            if (string.IsNullOrEmpty(_config?.MemoryTargetLogName)) return;
            _memoryLogTarget = LogManager.Configuration.FindTargetByName(_config.MemoryTargetLogName) as MemoryTarget;
            
            _logCleanupTimer = new Timer(state => {
                if (ContextStartTimestamps.Keys.Count == 0)
                {
                    _memoryLogTarget?.Logs?.Clear();
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        public void AddTimestamp(Guid contextId)
        {
            ContextStartTimestamps[contextId] = DateTime.Now;
        }

        public void RemoveTimestamp(Guid contextId)
        {
            ContextStartTimestamps.Remove(contextId, out _);
        }

        public DateTime GetTimestamp(Guid contextId)
        {
            return ContextStartTimestamps.GetOrAdd(contextId, _ => DateTime.Now);
        }

        public void GetPendingMemoryLogs()
        {
            if (_memoryLogTarget == null)
            {
                return;
            }

            var logCopy = new List<string>();
            
            _memoryLogTarget.Logs.ToList().ForEach(l => logCopy.Add(l));
            _memoryLogTarget.Logs.Clear();

            logCopy.Select(log => (DateTime.Now, new ContextLogEntry(-1, null, Guid.Empty, log, LogLevel.Information, TimeSpan.Zero, DateTime.UtcNow, ContextLogEntryType.OutOfContext)))
                .ToList()
                .ForEach(entry => OutOfContextLogs.Enqueue(entry));
        }

        public void RemoveIrrelevantMemoryLogs()
        {
            var timestamps = ContextStartTimestamps.Values.ToList();

            if (!timestamps.Any()) return;
            
            var minTimestamp = timestamps.Min();

            var currentLogs = OutOfContextLogs.ToList();
            currentLogs.Where(log => log.Timestamp < minTimestamp)
                .ToList()
                .ForEach(log => OutOfContextLogs.TryDequeue(out _));
            
        }

        public List<ContextLogEntry> GetMemoryLogsSince(DateTime timestamp)
        {
            var logs = OutOfContextLogs
                .ToList()
                .Where(log => log.Timestamp >= timestamp)
                .Select(log => log.LogEntry)
                .ToList();

            return logs;
        }
    }
}