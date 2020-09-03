using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ContextRunner.Base;
using ContextRunner.State;
using Newtonsoft.Json;

namespace ContextRunner
{
    public class ActionContextRunner : IContextRunner
    {
        public static ActionContextRunner Runner { get; protected set; }

        static ActionContextRunner()
        {
            Runner = new ActionContextRunner();
        }

        public static void Configure(
            Action<ActionContext> onStart = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> sanitizers = null)
        {
            Runner = new ActionContextRunner(onStart, settings, sanitizers);
        }

        protected Action<ActionContext> OnStart { get; set; }
        protected ActionContextSettings Settings { get; set; }
        protected IEnumerable<ISanitizer> Sanitizers { get; set; }

        public ActionContextRunner(
            Action<ActionContext> onStart = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> sanitizers = null)
        {
            OnStart = onStart ?? Setup;
            Settings = settings ?? new ActionContextSettings();
            Sanitizers = sanitizers ?? new ISanitizer[0];
        }

        private void Setup(ActionContext context)
        {
            context.Logger.WhenEntryLogged.Subscribe(
                _ => { },
                _ => LogContext(context),
                () => LogContext(context));
        }

        private void LogContext(ActionContext context)
        {
            if (!context.Logger.LogEntries.Any() || context.ShouldSuppress())
            {
                return;
            }

            var entry = context.Logger.GetSummaryLogEntry();
            var level = entry.LogLevel;

            var entries = context.Logger.LogEntries
                .Select(e => new
                {
                    Level = e.LogLevel,
                    Message = e.Message,
                    Context = e.ContextName,
                    e.TimeElapsed
                });

            var props = new Dictionary<object, object>
            {
                { "entries", entries },
                { "contextName", entry?.ContextName ?? context.ContextName },
                { "timeElapsed", entry?.TimeElapsed ?? context.TimeElapsed }
            };

            var stateParams = context.State.Params
                .Select(p => new KeyValuePair<string, object>($"{p.Key.Substring(0, 1).ToLower()}{p.Key.Substring(1)}", p.Value))
                .Where(p => p.Value != null)
                .Select(kvp =>
                {
                    var val = kvp.Value;

                    if (val is Exception ex)
                    {
                        ex.Data.Clear();

                        val = ex;
                    }

                    return new KeyValuePair<string, object>(kvp.Key, val);
                })
                .ToList();

            stateParams.ForEach(p => props.Add(p.Key, p.Value));

            var logObj = new
            {
                Time = DateTime.Now,
                Level = level,
                Name = context.ContextName,
                Message = entry.Message,
                Properties = props
            };

            var logline = JsonConvert.SerializeObject(logObj);

            Console.WriteLine(logline);
        }

        public void RunAction(Action<ActionContext> action, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            using (var context = new ActionContext(contextGroupName, name, Settings, Sanitizers))
            {
                try
                {
                    if (context.IsRoot)
                    {
                        OnStart?.Invoke(context);
                    }

                    action?.Invoke(context);
                }
                catch (Exception ex)
                {
                    throw HandleError(ex, context);
                }
            };
        }

        public async Task RunAction(Func<ActionContext, Task> action, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            using (var context = new ActionContext(contextGroupName, name, Settings, Sanitizers))
            {
                try
                {
                    if (context.IsRoot)
                    {
                        OnStart?.Invoke(context);
                    }

                    await action?.Invoke(context);
                }
                catch (Exception ex)
                {
                    throw HandleError(ex, context);
                }
            };
        }

        public async Task<T> RunAction<T>(Func<ActionContext, Task<T>> action, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            using (var context = new ActionContext(contextGroupName, name, Settings, Sanitizers))
            {
                try
                {
                    if (context.IsRoot)
                    {
                        OnStart?.Invoke(context);
                    }

                    return await action?.Invoke(context);
                }
                catch (Exception ex)
                {
                    throw HandleError(ex, context);
                }
            };
        }

        private Exception HandleError(Exception ex, ActionContext context)
        {
            var wasHandled = ex.Data.Contains("ContextExceptionHandled");

            if(ex != null && !wasHandled)
            {
                context.State.SetParam("Exception", ex);

                context.Logger.Log(Settings.ContextErrorMessageLevel,
                    $"An exception of type {ex.GetType().Name} was thrown within the context '{context.ContextName}'!");

                ex.Data.Add("ContextExceptionHandled", true);
                ex.Data.Add("ContextParams", context.State.Params);
                ex.Data.Add("ContextEntries", context.Logger.LogEntries.ToArray());

                context.Logger.ErrorToEmit = ex;
            }

            return ex;
        }
    }
}
