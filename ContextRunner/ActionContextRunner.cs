using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ContextRunner.Base;
using ContextRunner.State;

namespace ContextRunner
{
    public class ActionContextRunner : IContextRunner
    {
        protected Action<ActionContext> OnStart { get; set; }
        protected ActionContextSettings Settings { get; set; }
        protected IEnumerable<ISanitizer> Sanitizers { get; set; }

        public ActionContextRunner(
            Action<ActionContext> onStart = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> sanitizers = null)
        {
            OnStart = onStart;
            Settings = settings ?? new ActionContextSettings();
            Sanitizers = sanitizers ?? new ISanitizer[0];
        }

        public void RunAction(Action<ActionContext> action, [CallerMemberName]string name = null)
        {
            using (var context = new ActionContext(name, Settings, Sanitizers))
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

        public async Task RunAction(Func<ActionContext, Task> action, [CallerMemberName]string name = null)
        {
            using (var context = new ActionContext(name, Settings, Sanitizers))
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

        public async Task<T> RunAction<T>(Func<ActionContext, Task<T>> action, [CallerMemberName]string name = null)
        {
            using (var context = new ActionContext(name, Settings, Sanitizers))
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
            var toThrow = ex;

            if(ex != null && !(ex is ContextException))
            {
                context.State.SetParam("Exception", ex);

                context.Logger.Log(Settings.ContextErrorMessageLevel,
                    $"An exception of type {ex.GetType().Name} was thrown within the context '{context.ContextName}'!");

                toThrow = new ContextException(context, $"Error occurred within the context '{context.ContextName}'!", ex);
            }

            return context.IsRoot && !Settings.WrapExceptionsWithContext && toThrow is ContextException
                ? toThrow.InnerException
                : toThrow;

        }
    }
}
