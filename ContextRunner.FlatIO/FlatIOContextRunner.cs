﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ContextRunner.Base;
using ContextRunner.Logging;

namespace ContextRunner.FlatIO
{
    public class FlatIOContextRunner : ContextRunner
    {
        private readonly FlatIOContextRunnerConfig _config;

        private readonly string _seperator;

        public FlatIOContextRunner(IOptionsMonitor<FlatIOContextRunnerConfig> flatContextOptions) {
            _config = flatContextOptions.CurrentValue;

            _seperator = string.Empty.PadLeft(100, '=');

            OnStart = Setup;
        }

        private void Setup(ActionContext context)
        {
            context.Logger.WhenEntryLogged.Subscribe(
                _ => { },
                exception => LogContext(context),
                () => LogContext(context));
        }

        private void LogContext(ActionContext context)
        {
            if (!context.Logger.LogEntries.Any())
            {
                return;
            }

            var logDir = _config.LogDir ?? "./logs";
            logDir = logDir.StartsWith("./")
                ? $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}{logDir.Substring(1)}"
                : logDir;

            var baseLogName = _config.BaseLogName ?? "flatcontext";
            var dateSuffix = DateTime.UtcNow.ToString("yyyyMMdd");

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            var contextName = $"Context: {context.ContextName}";
            var timestamp = $"Timestamp: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff")}";

            var entry = GetSummaryLogEntry(context);
            var totalTime = $"TimeElapsed: {entry.TimeElapsed}";
            var highestLevel = $"Level: {GetShortLogLevel(entry.LogLevel)}";
            var summary = $"Summary: {entry.Message}";

            var output = new StreamWriter($"{logDir}/{baseLogName}-{dateSuffix}.log", true);
            output.WriteLine(_seperator);
            output.WriteLine($" {contextName}");
            output.WriteLine($" {timestamp}");
            output.WriteLine($" {totalTime}");
            output.WriteLine($" {highestLevel}");
            output.WriteLine($" {summary}");
            output.WriteLine(_seperator);

            foreach(var p in context.State.Params)
            {
                output.WriteLine($"\t{p.Key}: {p.Value}");
            }

            output.WriteLine(_seperator);

            foreach (var e in context.Logger.LogEntries)
            {
                output.WriteLine($"{e.TimeElapsed} {GetShortLogLevel(e.LogLevel).PadRight(5)} {AddSpacing(e)}");
            }

            output.WriteLine(_seperator);

            output.Flush();
            output.Close();
        }

        private ContextLogEntry GetSummaryLogEntry(ActionContext context)
        {
            var entries = context.Logger.LogEntries.ToList();

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
                highest = entries.Where(entry => entry.LogLevel == level);

                if (highest.Any())
                {
                    break;
                }
            }

            var highestLevel = highest.FirstOrDefault()?.LogLevel ?? LogLevel.Trace;
            var message = $"The context '{context.ContextName}' ended without error.";

            if (highestLevel == LogLevel.Error || highestLevel == LogLevel.Critical)
            {
                message = highest.Count() == 1
                    ? $"The context '{context.ContextName}' ended with an error. {highest.First().Message}"
                    : $"The context '{context.ContextName}' ended with multiple errors.";
            }
            else if (highestLevel == LogLevel.Warning)
            {
                message = highest.Count() == 1
                    ? $"The context '{context.ContextName}' ended with a warning. {highest.First().Message}"
                    : $"The context '{context.ContextName}' ended with multiple warnings.";
            }

            var result = new ContextLogEntry(0, context.ContextName, message, highestLevel, context.TimeElapsed);

            return result;
        }

        private string GetShortLogLevel(LogLevel level)
        {
            if(level == LogLevel.Critical)
            {
                return "FATAL";
            }
            else if(level == LogLevel.Warning || level == LogLevel.Information)
            {
                return level.ToString().ToUpper().Substring(0, 4);
            }
            else
            {
                return level.ToString().ToUpper();
            }
        }

        private string AddSpacing(ContextLogEntry entry)
        {
            var spacing = "";

            for (var x = 0; x < entry.ContextDepth; x++)
            {
                spacing += "\t";
            }

            return spacing + entry.Message;
        }
    }
}
