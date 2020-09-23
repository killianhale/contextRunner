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
            Action<IActionContext> onStart = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> sanitizers = null)
        {
            Runner = new ActionContextRunner(onStart, settings, sanitizers);
        }

        protected Action<IActionContext> OnStart { get; set; }
        protected Action<IActionContext> OnEnd{ get; set; }
        protected ActionContextSettings Settings { get; set; }
        protected IEnumerable<ISanitizer> Sanitizers { get; set; }
        
        private IDisposable _logHandle;

        public ActionContextRunner(
            Action<IActionContext> onStart = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> sanitizers = null)
        {
            OnStart = onStart ?? Setup;
            Settings = settings ?? new ActionContextSettings();
            Sanitizers = sanitizers ?? new ISanitizer[0];
        }
        
        #region Base implementation stuff...

        private void Setup(IActionContext context)
        {
            _logHandle = context.Logger.WhenEntryLogged.Subscribe(
                _ => { },
                _ => LogContext(context),
                () => LogContext(context));
        }

        private void LogContext(IActionContext context)
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
                    Level = e.LogLevel.ToString(),
                    Message = e.Message,
                    Context = e.ContextName,
                    e.TimeElapsed
                });

            var props = new Dictionary<object, object>
            {
                { "entries", entries },
                { "contextName", entry?.ContextName ?? context.ContextName },
                { "contextId", context.Id },
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
                Level = level.ToString(),
                Name = context.ContextName,
                Message = entry.Message,
                Properties = props
            };

            var logline = JsonConvert.SerializeObject(logObj);

            Console.WriteLine(logline);
        }

        public virtual void Dispose()
        {
            _logHandle?.Dispose();
        }
        
        #endregion

        public IActionContext Create([CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            var context =  new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            if (context.IsRoot)
            {
                OnStart?.Invoke(context);
            }
            
            context.OnDispose = c =>
            {
                OnEnd?.Invoke(c);
            };

            return context;
        }

        [Obsolete("Please use CreateAndWrapActionExceptions as its use is clearer.", false)]
        public void RunAction(Action<IActionContext> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            CreateAndAppendToActionExceptions(action, name, contextGroupName);
        }


        [Obsolete("Please use CreateAndWrapActionExceptions as its use is clearer.", false)]
        public T RunAction<T>(Func<IActionContext, T> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            return CreateAndAppendToActionExceptions(action, name, contextGroupName);
        }

        [Obsolete("Please use CreateAndAppendToActionExceptions as its use is clearer.", false)]
        public async Task RunAction(Func<IActionContext, Task> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            await CreateAndAppendToActionExceptions(action, name, contextGroupName);
        }

        [Obsolete("Please use CreateAndAppendToActionExceptions as its use is clearer.", false)]
        public async Task<T> RunAction<T>(Func<IActionContext, Task<T>> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            return await CreateAndAppendToActionExceptions(action, name, contextGroupName);
        }

        public void CreateAndAppendToActionExceptions(Action<IActionContext> action,
            [CallerMemberName] string name = null, string contextGroupName = "default")
        {
            CreateAndAppendToActionExceptions(action, HandleError, name, contextGroupName);
        }
        
        public void CreateAndAppendToActionExceptions(Action<IActionContext> action, Func<Exception, IActionContext, Exception> errorHandlingOverride, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            if (action == null) return;

            using var context = new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            try
            {
                if (context.IsRoot)
                {
                    OnStart?.Invoke(context);
                }

                action.Invoke(context);
                    
                OnEnd?.Invoke(context);
            }
            catch (Exception ex)
            {
                var handleError = errorHandlingOverride ?? HandleError;
                throw handleError(ex, context);
            }
        }

        public T CreateAndAppendToActionExceptions<T>(Func<IActionContext, T> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            return CreateAndAppendToActionExceptions(action, HandleError, name, contextGroupName);
        }
        
        public T CreateAndAppendToActionExceptions<T>(Func<IActionContext, T> action, Func<Exception, IActionContext, Exception> errorHandlingOverride, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            if (action == null) return default;

            using var context = new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            try
            {
                if (context.IsRoot)
                {
                    OnStart?.Invoke(context);
                }

                var result = action.Invoke(context);
                    
                OnEnd?.Invoke(context);

                return result;
            }
            catch (Exception ex)
            {
                var handleError = errorHandlingOverride ?? HandleError;
                throw handleError(ex, context);
            }
        }

        public async Task CreateAndAppendToActionExceptions(Func<IActionContext, Task> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            await CreateAndAppendToActionExceptions(action, HandleError, name, contextGroupName);
        }

        public async Task CreateAndAppendToActionExceptions(Func<IActionContext, Task> action, Func<Exception, IActionContext, Exception> errorHandlingOverride, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            if (action == null) return;

            using var context = new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            try
            {
                if (context.IsRoot)
                {
                    OnStart?.Invoke(context);
                }

                await action.Invoke(context);
                    
                OnEnd?.Invoke(context);
            }
            catch (Exception ex)
            {
                var handleError = errorHandlingOverride ?? HandleError;
                throw handleError(ex, context);
            }
        }

        public async Task<T> CreateAndAppendToActionExceptions<T>(Func<IActionContext, Task<T>> action, [CallerMemberName] string name = null,
            string contextGroupName = "default")
        {
            return await CreateAndAppendToActionExceptions(action, HandleError, name, contextGroupName);
        }

        public async Task<T> CreateAndAppendToActionExceptions<T>(Func<IActionContext, Task<T>> action, Func<Exception, IActionContext, Exception> errorHandlingOverride, [CallerMemberName]string name = null, string contextGroupName = "default")
        {
            if (action == null) return default;
            
            using var context = new ActionContext(contextGroupName, name, Settings, Sanitizers);
            context.State.SetParam("RunnerType", this.GetType().Name);
            
            try
            {
                if (context.IsRoot)
                {
                    OnStart?.Invoke(context);
                }

                var result = await action.Invoke(context);
                    
                OnEnd?.Invoke(context);

                return result;
            }
            catch (Exception ex)
            {
                var handleError = errorHandlingOverride ?? HandleError;
                throw handleError(ex, context);
            }
        }

        private Exception HandleError(Exception ex, IActionContext context)
        {
            var wasHandled = ex?.Data.Contains("ContextExceptionHandled");

            if (ex == null || wasHandled == true) return ex;
            
            context.State.SetParam("Exception", ex);

            context.Logger.Log(Settings.ContextErrorMessageLevel,
                $"An exception of type {ex.GetType().Name} was thrown within the context '{context.ContextName}'!");

            ex.Data.Add("ContextExceptionHandled", true);
            ex.Data.Add("ContextParams", context.State.Params);
            ex.Data.Add("ContextEntries", context.Logger.LogEntries.ToArray());

            context.Logger.ErrorToEmit = ex;

            return ex;
        }
    }
}
