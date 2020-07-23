using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Microsoft.Extensions.Options;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;
using ContextRunner.Logging;
using ContextRunner.State;
using ContextRunner.State.Sanitizers;
using ContextRunner.Base;

namespace ContextRunner.NLog
{
    public class NlogContextRunner : ActionContextRunner
    {
        public static void Configure(NlogContextRunnerConfig config)
        {
            Runner = new NlogContextRunner(config);
        }

        private readonly NlogContextRunnerConfig _config;

        public NlogContextRunner(NlogContextRunnerConfig config)
        {
            _config = config;

            OnStart = Setup;
            Settings = GetActionContextSettings();
            Sanitizers = _config?.SanitizedProperties != null && _config.SanitizedProperties.Length > 0
                ? new[] { new KeyBasedSanitizer(_config.SanitizedProperties) }
                : new[] { new KeyBasedSanitizer(new string[0]) };
        }

        public NlogContextRunner(IOptionsMonitor<NlogContextRunnerConfig> options)
        {
            _config = options.CurrentValue;

            OnStart = Setup;
            Settings = GetActionContextSettings();
            Sanitizers = _config?.SanitizedProperties != null && _config.SanitizedProperties.Length > 0
                ? new[] { new KeyBasedSanitizer(_config.SanitizedProperties) }
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
                IgnoreChildSuppressionOnError = _config.IgnoreChildSuppressionOnError,
                ContextEndMessageLevel = ConvertToMsLogLevel(_config.ContextEndMessageLevel),
                ContextStartMessageLevel = ConvertToMsLogLevel(_config.ContextStartMessageLevel),
                ContextErrorMessageLevel = ConvertToMsLogLevel(_config.ContextErrorMessageLevel),
                SuppressContextByNameList = _config.SuppressContextByNameList,
                SuppressContextsByNameUnderLevel = ConvertToMsLogLevel(_config.SuppressContextsByNameUnderLevel)
            };
        }

        private void Setup(ActionContext context)
        {
            context.Logger.WhenEntryLogged.Subscribe(
                entry => LogEntry(context, entry),
                exception => LogContextWithError(context, exception),
                () => LogContext(context));
        }

        private void LogEntry(ActionContext context, ContextLogEntry entry)
        {
            var prefix = _config.EntryLogNamePrefix ?? "entry_";
            var logger = LogManager.GetLogger(prefix + entry.ContextName);

            var level = ConvertFromMsLogLevel(entry.LogLevel);
            var eventParams = GetEventParams(context, entry);

            var e = new LogEventInfo(level, logger.Name, entry.Message);
            eventParams.ToList().ForEach(x => e.Properties.Add(x.Key, x.Value));

            logger.Log(e);
        }

        private void LogContextWithError(ActionContext context, Exception ex)
        {
            LogContext(context);
        }

        private void LogContext(ActionContext context)
        {
            if(!context.Logger.LogEntries.Any() || context.ShouldSuppress())
            {
                return;
            }

            var entry = context.Logger.GetSummaryLogEntry();
            var level = ConvertFromMsLogLevel(entry.LogLevel);
            var eventParams = GetEventParams(context);

            var prefix = _config.ContextLogNamePrefix ?? "context_";
            var logger = LogManager.GetLogger(prefix + context.ContextName);

            var e = new LogEventInfo(level, logger.Name, entry.Message);
            eventParams.ToList().ForEach(x => e.Properties.Add(x.Key, x.Value));

            var entries = context.Logger.LogEntries
                .Select(e => new
                {
                    Level = ConvertFromMsLogLevel(e.LogLevel).ToString(),
                    Message = AddSpacing(e),
                    Context = e.ContextName,
                    e.TimeElapsed
                });

            e.Properties.Add("entries", entries);

            logger.Log(e);
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

        private IDictionary<object, object> GetEventParams(ActionContext context, ContextLogEntry entry = null)
        {
            var result = new Dictionary<object, object>();

            if(entry != null)
            {
                result.Add("contextDepth", entry?.ContextDepth ?? context.Depth);
            }

            result.Add("contextName", entry?.ContextName ?? context.ContextName);
            result.Add("timeElapsed", entry?.TimeElapsed ?? context.TimeElapsed);

            var sanitizedParams = context.State.Params
                .Select(p => new KeyValuePair<string, object>($"{p.Key.Substring(0, 1).ToLower()}{p.Key.Substring(1)}", p.Value))
                .Where(p => p.Value != null)
                .ToList();

            sanitizedParams.ForEach(p => result.Add(p.Key, p.Value));

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
    }
}
