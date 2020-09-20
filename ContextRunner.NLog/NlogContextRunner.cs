using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Microsoft.Extensions.Options;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;
using ContextRunner.Logging;
using ContextRunner.State;
using ContextRunner.State.Sanitizers;
using ContextRunner.Base;
using NLog.Config;
using NLog.Targets;

namespace ContextRunner.NLog
{
    public class NlogContextRunner : ActionContextRunner, IDisposable
    {
        public static void Configure(NlogContextRunnerConfig config)
        {
            Runner = new NlogContextRunner(config);
        }
        
        public static readonly ConcurrentQueue<(DateTime Timestamp, ContextLogEntry LogEntry)> OutOfContextLogs = new ConcurrentQueue<(DateTime, ContextLogEntry)>();
        public static readonly ConcurrentDictionary<Guid, DateTime> ContextStartTimestamps = new ConcurrentDictionary<Guid, DateTime>();

        private readonly NlogContextRunnerConfig _config;
        private readonly MemoryTarget _memoryLogTarget;
        private IDisposable _logHandle;

        public NlogContextRunner(IOptionsMonitor<NlogContextRunnerConfig> options) : this(options.CurrentValue)
        {
        }

        public NlogContextRunner(NlogContextRunnerConfig config)
        {
            _config = config;

            OnStart = Setup;
            OnEnd = Teardown;
            Settings = GetActionContextSettings();
            Sanitizers = _config?.SanitizedProperties != null && _config.SanitizedProperties.Length > 0
                ? new[] { new KeyBasedSanitizer(_config.SanitizedProperties) }
                : new[] { new KeyBasedSanitizer(new string[0]) };

            if (!string.IsNullOrEmpty(_config?.MemoryTargetLogName))
            {
                _memoryLogTarget = LogManager.Configuration.FindTargetByName(_config.MemoryTargetLogName) as MemoryTarget;
            }
        }

        private ActionContextSettings GetActionContextSettings()
        {
            return new ActionContextSettings
            {
                EnableContextEndMessage = _config.EnableContextEndMessage,
                EnableContextStartMessage = _config.EnableContextStartMessage,
                SuppressChildContextEndMessages = _config.SuppressChildContextEndMessages,
                SuppressChildContextStartMessages = _config.SuppressChildContextStartMessages,
                IgnoreChildSuppressionOnError = _config.IgnoreChildSuppressionOnError,
                ContextEndMessageLevel = ConvertToMsLogLevel(_config.ContextEndMessageLevel),
                ContextStartMessageLevel = ConvertToMsLogLevel(_config.ContextStartMessageLevel),
                ContextErrorMessageLevel = ConvertToMsLogLevel(_config.ContextErrorMessageLevel),
                SuppressContextByNameList = _config.SuppressContextByNameList,
                SuppressContextsByNameUnderLevel = ConvertToMsLogLevel(_config.SuppressContextsByNameUnderLevel)
            };
        }

        private void Setup(IActionContext context)
        {
            ContextStartTimestamps[context.Id] = DateTime.Now;
            
            _logHandle = context.Logger.WhenEntryLogged.Subscribe(
                entry => LogEntry(context, entry),
                exception => LogContextWithError(context, exception),
                () => LogContext(context));
        }

        private void Teardown(IActionContext context)
        {
            LogManager.Flush();
            
            RemoveIrrelevantMemoryLogs();
        }

        private void LogEntry(IActionContext context, ContextLogEntry entry)
        {
            GetPendingMemoryLogs();
            
            var prefix = _config.EntryLogNamePrefix ?? "entry_";
            var logger = LogManager.GetLogger(prefix + entry.ContextName);

            var level = ConvertFromMsLogLevel(entry.LogLevel);
            var eventParams = GetEventParams(context, false, entry);

            var e = new LogEventInfo(level, logger.Name, entry.Message);
            eventParams.ToList().ForEach(x => e.Properties.Add(x.Key, x.Value));

            logger.Log(e);
            LogManager.Flush();
        }

        private void LogContextWithError(IActionContext context, Exception ex)
        {
            LogContext(context);
        }

        private void LogContext(IActionContext context)
        {
            if(context.ShouldSuppress())
            {
                return;
            }
            
            GetPendingMemoryLogs();
            
            if(!context.Logger.LogEntries.Any())
            {
                return;
            }

            var entry = context.Logger.GetSummaryLogEntry();
            var level = ConvertFromMsLogLevel(entry.LogLevel);
            var eventParams = GetEventParams(context, true);

            var prefix = _config.ContextLogNamePrefix ?? "context_";
            var logger = LogManager.GetLogger(prefix + context.ContextName);

            var @event = new LogEventInfo(level, logger.Name, entry.Message);
            eventParams.ToList().ForEach(x =>  @event.Properties.Add(x.Key, x.Value));

            var timestamp = ContextStartTimestamps.GetOrAdd(context.Id, _ => DateTime.Now);
            var logs = GetMemoryLogsSince(timestamp);
            
            logs.AddRange(context.Logger.LogEntries);
            
            var entries = logs
                .Select(e => new
                {
                    Level = ConvertFromMsLogLevel(e.LogLevel).ToString(),
                    Message = AddSpacing(e),
                    Context = e.ContextName,
                    e.TimeElapsed
                });

            @event.Properties.Add("entries", entries);

            logger.Log(@event);

            LogManager.Flush();
            
            ContextStartTimestamps.Remove(context.Id, out _);
        }

        private string AddSpacing(ContextLogEntry entry)
        {
            if(_config?.AddSpacingToEntries == null || !_config.AddSpacingToEntries)
            {
                return entry.Message;
            }

            var spacing = "";

            for (var x = 0; x < entry.ContextDepth; x++)
            {
                spacing += "\t";
            }

            return spacing + entry.Message;
        }

        private IDictionary<object, object> GetEventParams(IActionContext context, bool isContext, ContextLogEntry entry = null)
        {
            var result = new Dictionary<object, object>();

            if(entry != null)
            {
                result.Add("contextDepth", entry?.ContextDepth ?? context.Depth);
            }

            result.Add("timeElapsed", entry?.TimeElapsed ?? context.TimeElapsed);
            if (isContext)
            {
                result.Add("contextGroupName", context.ContextGroupName);
                result.Add("baseContextName", context.ContextName);
                result.Add("contextId", context.Id);
            }
            else
            {
                result.Add("contextName", entry?.ContextName ?? context.ContextName);
                result.Add("contextId", context.Id);
                result.Add("contextCausationId", context.CausationId);
                result.Add("contextCorrelationId", context.CorrelationId);
            }

            foreach (var kvp in context.State.Params)
            {
                if (!(kvp.Value is Exception ex)) continue;
                
                ex.Data["ContextParams"] = null;
                ex.Data["ContextEntries"] = null;
            }

            var sanitizedParams = context.State.Params
                .Select(p => new KeyValuePair<string, object>($"{p.Key.Substring(0, 1).ToLower()}{p.Key.Substring(1)}", p.Value))
                .Where(p => p.Value != null)
                .ToList();

            sanitizedParams.ForEach(p => result.Add(p.Key, p.Value));

            LogManager.Flush();

            return result;
        }

        private LogLevel ConvertFromMsLogLevel(MsLogLevel level)
        {
            return level switch
            {
                MsLogLevel.Trace => LogLevel.Trace,
                MsLogLevel.Debug => LogLevel.Debug,
                MsLogLevel.Information => LogLevel.Info,
                MsLogLevel.Warning => LogLevel.Warn,
                MsLogLevel.Error => LogLevel.Error,
                MsLogLevel.Critical => LogLevel.Fatal,
                _ => LogLevel.Off,
            };
        }

        private MsLogLevel ConvertToMsLogLevel(LogLevel level)
        {
            var result = MsLogLevel.None;

            if (level == LogLevel.Trace)
            {
                result = MsLogLevel.Trace;
            }
            else if (level == LogLevel.Debug)
            {
                result = MsLogLevel.Debug;
            }
            else if (level == LogLevel.Info)
            {
                result = MsLogLevel.Information;
            }
            else if (level == LogLevel.Warn)
            {
                result = MsLogLevel.Warning;
            }
            else if (level == LogLevel.Error)
            {
                result = MsLogLevel.Error;
            }
            else if (level == LogLevel.Fatal)
            {
                result = MsLogLevel.Critical;
            }

            return result;
        }

        private void GetPendingMemoryLogs()
        {
            if (_memoryLogTarget == null)
            {
                return;
            }

            var logCopy = new List<string>();
            
            _memoryLogTarget.Logs.ToList().ForEach(l => logCopy.Add(l));
            _memoryLogTarget.Logs.Clear();

            logCopy.Select(log => (DateTime.Now, new ContextLogEntry(-1, null, log, MsLogLevel.Information, TimeSpan.Zero, false)))
                .ToList()
                .ForEach(entry => OutOfContextLogs.Enqueue(entry));
        }

        private void RemoveIrrelevantMemoryLogs()
        {
            var timestamps = ContextStartTimestamps.Values.ToList();

            if (!timestamps.Any()) return;
            
            var minTimestamp = timestamps.Min();

            var currentLogs = OutOfContextLogs.ToList();
            currentLogs.Where(log => log.Timestamp < minTimestamp)
                .ToList()
                .ForEach(log => OutOfContextLogs.TryDequeue(out _));
            
        }

        private List<ContextLogEntry> GetMemoryLogsSince(DateTime timestamp)
        {
            var logs = OutOfContextLogs
                .ToList()
                .Where(log => log.Timestamp >= timestamp)
                .Select(log => log.LogEntry)
                .ToList();

            return logs;
        }

        public override void Dispose()
        {
            Teardown(null);
            LogManager.Shutdown();
            _logHandle?.Dispose();
            
            base.Dispose();
        }
    }
}
