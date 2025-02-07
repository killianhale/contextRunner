using System;
using System.Linq;
using NLog;
using Microsoft.Extensions.Options;
using ContextRunner.Logging;
using ContextRunner.State.Sanitizers;
using ContextRunner.Base;
using ContextRunner.NLog.Internal;

namespace ContextRunner.NLog
{
    public class NlogContextRunner : ActionContextRunner
    {
        public static void Configure(NlogContextRunnerConfig config)
        {
            Runner = new NlogContextRunner(config);
        }
        
        private readonly NlogContextRunnerConfig _config = new ();
        private readonly MemoryLogService _memoryLogService;
        private IDisposable? _logHandle;

        public NlogContextRunner(IOptionsMonitor<NlogContextRunnerConfig> options) : this(options.CurrentValue)
        {
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public NlogContextRunner(NlogContextRunnerConfig? config)
        {
            if (config != null)
            {
                _config = config;
            }
            
            _memoryLogService = new MemoryLogService(_config);

            var maxDepth = config?.MaxSanitizerDepth ?? 10;

            OnStart = Setup;
            OnEnd = Teardown;
            Settings = GetActionContextSettings();
            Sanitizers = _config.SanitizedProperties is { Length: > 0 }
                ? new[] { new KeyBasedSanitizer(_config.SanitizedProperties, maxDepth) }
                : new[] { new KeyBasedSanitizer(Array.Empty<string>()) };
        }

        private ActionContextSettings GetActionContextSettings()
        {
            // _config ??= new NlogContextRunnerConfig();
            
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
                SuppressContextsByNameUnderLevel = NlogLogLevelUtil.ConvertToMsLogLevel(_config.SuppressContextsByNameUnderLevel),
                PropertiesToAddToSummaryList = _config.PropertiesToAddToSummaryList
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

        private void Teardown(IActionContext? context)
        {
            LogManager.Flush();
            
            _memoryLogService.RemoveIrrelevantMemoryLogs();
        }

        private void LogEntry(IActionContext context, ContextLogEntry entry)
        {
            _memoryLogService.GetPendingMemoryLogs();
            
            var prefix = _config.EntryLogNamePrefix;
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
            context.State.SetParam("Exception", ex);
            
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

            var prefix = _config.ContextLogNamePrefix;
            
            var summaries = ContextSummary.Summarize(context);
            
            summaries.ForEach(summary =>
            {
                var contextInfo = summary.Data["contextInfo"] as IRootContextInfo;
                var checkpoint = contextInfo?.Checkpoint;
                checkpoint = checkpoint == null ? string.Empty : $"_{checkpoint}";

                summary.Data["contextInfo"] = contextInfo == null
                    ? null
                    : new
                {
                    contextInfo.Checkpoint,
                    contextInfo.Id,
                    contextInfo.ContextName,
                    contextInfo.ContextGroupName
                };
                
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
                    e.Timestamp,
                    Level = NlogLogLevelUtil.ConvertFromMsLogLevel(e.LogLevel).ToString(),
                    e.Message,
                    e.ContextName,
                    e.ContextId,
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
            
            GC.SuppressFinalize(this);
        }
    }
}
