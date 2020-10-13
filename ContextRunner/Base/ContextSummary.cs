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
            var message = context.Settings.PropertiesToAddToSummaryList
                // .Where(p => context.State.ContainsKey(p))
                .Aggregate(summary.Message, (current, p) =>
                {
                    var keys = p.Split(':', StringSplitOptions.RemoveEmptyEntries);

                    object lookup = null;

                    for (var x = 0; x < keys.Length; x++)
                    {
                        if (x == 0 && context.State.ContainsKey(keys[x]))
                        {
                            lookup = context.State.GetParam(keys[x]);
                        }
                        else if (lookup == null)
                        {
                            break;
                        }
                        else if (lookup is IDictionary<string, object> dict)
                        {
                            lookup = dict.ContainsKey(keys[x])
                                ? dict[keys[x]]
                                : null;
                        }
                        else
                        {
                            lookup = lookup?.GetType()?.GetProperty(keys[x]);
                        }
                    }

                    var result = lookup != null
                        ? current + $" {p}: {lookup}"
                        : current;

                    return result;
                });

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
            
            data.Add("timeElapsed", context.TimeElapsed);
            data.Add("contextInfo", context.Info);
            
            return new ContextSummary(
                summary.Timestamp,
                summary.LogLevel,
                message,
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