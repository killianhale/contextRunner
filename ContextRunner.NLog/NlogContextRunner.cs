using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using Microsoft.Extensions.Options;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;
using ContextRunner.Logging;
using ContextRunner.State;
using ContextRunner.State.Sanitizers;
using ContextRunner.Base;
using ContextRunner.NLog.Internal;
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
        
        private readonly NlogContextRunnerConfig _config;
        private readonly IMemoryLogService _memoryLogService;
        private IDisposable _logHandle;

        public NlogContextRunner(IOptionsMonitor<NlogContextRunnerConfig> options) : this(options.CurrentValue)
        {
        }

        public NlogContextRunner(NlogContextRunnerConfig config)
        {
            _config = config;
            _memoryLogService = new MemoryLogService(config);

            var maxDepth = config?.MaxSanitizerDepth ?? 10;

            OnStart = Setup;
            OnEnd = Teardown;
            Settings = GetActionContextSettings();
            Sanitizers = _config?.SanitizedProperties != null && _config.SanitizedProperties.Length > 0
                ? new[] { new KeyBasedSanitizer(_config.SanitizedProperties, maxDepth) }
                : new[] { new KeyBasedSanitizer(new string[0]) };
        }

        private ActionContextSettings GetActionContextSettings()
        {
            return new ActionContextSettings
            {
                EnableContextEndMessage = _config.EnableContextEndMessage,
                EnableContextStartMessage = _config.EnableContextStartMessage,
                SuppressChildContextEndMessages = _config.SuppressChildContextEndMessages,
                SuppressChildContextStartMessages = _config.SuppressChildContextStartMessages,
                AlwaysShowContextEndMessagesOnError = _config.AlwaysShowContextEndMessagesOnError,
                AlwaysShowContextStartMessagesOnError = _config.AlwaysShowContextStartMessagesOnError,
                ContextEndMessageLevel = NlogLogLevelUtil.ConvertToMsLogLevel(_config.ContextEndMessageLevel),
                ContextStartMessageLevel = NlogLogLevelUtil.ConvertToMsLogLevel(_config.ContextStartMessageLevel),
                ContextErrorMessageLevel = NlogLogLevelUtil.ConvertToMsLogLevel(_config.ContextErrorMessageLevel),
                SuppressContextByNameList = _config.SuppressContextByNameList,
                SuppressContextsByNameUnderLevel = NlogLogLevelUtil.ConvertToMsLogLevel(_config.SuppressContextsByNameUnderLevel)
            };
        }

        private void Setup(IActionContext context)
        {
            _memoryLogService.AddTimestamp(context.Info.Id);
            
            _logHandle = context.Logger.WhenEntryLogged.Subscribe(
                entry => LogEntry(context, entry),
                exception => LogContextWithError(context, exception),
                () => LogContext(context));
        }

        private void Teardown(IActionContext context)
        {
            LogManager.Flush();
            
            _memoryLogService.RemoveIrrelevantMemoryLogs();
        }

        private void LogEntry(IActionContext context, ContextLogEntry entry)
        {
            _memoryLogService.GetPendingMemoryLogs();
            
            var prefix = _config.EntryLogNamePrefix ?? "entry_";
            var logger = LogManager.GetLogger(prefix + entry.ContextName);

            var level = NlogLogLevelUtil.ConvertFromMsLogLevel(entry.LogLevel);
            var summary = ContextSummary.CreateFromContext(context);

            var e = new LogEventInfo(level, logger.Name, entry.Message);
            summary.Data.ToList().ForEach(x => e.Properties.Add(x.Key, x.Value));

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
            
            _memoryLogService.GetPendingMemoryLogs();
            
            var shouldLog = context.GetCheckpoints().Any() || context.Logger.LogEntries.Any();
            
            if (!shouldLog)
            {
                return;
            }

            var prefix = _config.ContextLogNamePrefix ?? "context_";
            
            var summaries = ContextSummary.Summarize(context);
            
            summaries.ForEach(summary =>
            {
                var contextInfo = summary.Data["contextInfo"] as IContextInfo;
                var checkpoint = contextInfo?.Checkpoint;
                checkpoint = checkpoint == null ? string.Empty : $"_{checkpoint}";
                
                var logger = LogManager.GetLogger($"{prefix}{context.Info.ContextName}{checkpoint}" );
                
                var entry = CreateLogEntry(
                    logger.Name,
                    context.Info.Id,
                    summary
                );
                
                logger.Log(entry);
            });
            
            LogManager.Flush();
            
            _memoryLogService.RemoveTimestamp(context.Info.Id);
        }

        private LogEventInfo CreateLogEntry(
            string nlogLoggerName,
            Guid contextId,
            ContextSummary summary
           )
        {
            var level = NlogLogLevelUtil.ConvertFromMsLogLevel(summary.Level);

            var @event = new LogEventInfo(level, nlogLoggerName, summary.Message);
            summary.Data.ToList().ForEach(x =>  @event.Properties.Add(x.Key, x.Value));

            var timestamp = _memoryLogService.GetTimestamp(contextId);
            var logs = _memoryLogService.GetMemoryLogsSince(timestamp);
            
            logs.AddRange(summary.Entries);
            
            var entries = logs
                .OrderBy(e => e.Timestamp)
                .Select(e => new
                {
                    Timestamp = e.Timestamp,
                    Level = NlogLogLevelUtil.ConvertFromMsLogLevel(e.LogLevel).ToString(),
                    Message = e.Message,
                    ContextName = e.ContextName,
                    ContextId = e.ContextId,
                    e.TimeElapsed
                });

            @event.Properties.Add("entries", entries);

            return @event;
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
