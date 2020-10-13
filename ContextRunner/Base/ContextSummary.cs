using System;
using System.Collections.Generic;
using System.Linq;
using ContextRunner.Logging;
using ContextRunner.State;
using Microsoft.Extensions.Logging;

namespace ContextRunner.Base
{
    public class ContextSummary
    {
        public static List<ContextSummary> Summarize(IActionContext context)
        {
            var checkpoints = context.GetCheckpoints();
            var summaries = checkpoints;
            
            summaries.Add(CreateFromContext(context));

            return summaries;
        }

        public static ContextSummary CreateFromContext(IActionContext context)
        {
            var summary = context.Logger.GetSummaryLogEntry();
            
            var data = new Dictionary<string, object>();

            var sanitizedParams = context.State.Params
                .Select(p => new KeyValuePair<string, object>($"{p.Key.Substring(0, 1).ToLower()}{p.Key.Substring(1)}", p.Value))
                .Where(p => p.Value != null)
                .ToList();
            
            foreach (var kvp in sanitizedParams)
            {
                if (!(kvp.Value is Exception ex)) continue;
                
                ex.Data["ContextParams"] = null;
                ex.Data["ContextEntries"] = null;
            }

            sanitizedParams.ForEach(p => data.Add(p.Key, p.Value));
            
            data.Add("contextInfo", context.Info);
            data.Add("timeElapsed", context.TimeElapsed);
            
            return new ContextSummary(
                summary.Timestamp,
                summary.LogLevel,
                summary.Message,
                context.Logger.LogEntries.ToList(),
                data
            );
        }

        protected ContextSummary(
            DateTime timestamp,
            LogLevel level,
            string message,
            List<ContextLogEntry> entries,
            Dictionary<string, object> data)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
            Entries = entries;
            Data = data;
        }
        public DateTime Timestamp { get; }
        public LogLevel Level { get; }
        public string Message { get; }
        
        public List<ContextLogEntry> Entries { get; }
        public Dictionary<string, object> Data { get; }
    }
}