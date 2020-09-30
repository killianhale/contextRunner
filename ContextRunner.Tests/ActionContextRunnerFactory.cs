using System;
using System.Threading;
using System.Threading.Tasks;
using ContextRunner.Base;
using ContextRunner.State;

namespace ContextRunner.Tests
{
    public class ActionContextRunnerFactory
    {
        private readonly ActionContextSettings _settings;
        private readonly ISanitizer[] _sanitizers;

        public ActionContextRunnerFactory(ActionContextSettings settings = null, ISanitizer[] sanitizers = null)
        {
            _settings = settings ?? new ActionContextSettings()
            {
                EnableContextEndMessage = false
            };
            _sanitizers = sanitizers ?? new ISanitizer[0];
        }

        public IActionContext Create(Action<IContextRunner> test)
        {
            var resetEvent = new AutoResetEvent(false);
            
            IDisposable logHandle = null;
            IActionContext completedContext = null;

            var contextRunner = new ActionContextRunner(c =>
                {
                    logHandle = c.Logger.WhenEntryLogged.Subscribe(
                        entry => { },
                        error =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        },
                        () =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        }
                    );
                }, _settings, _sanitizers);
            
            test.Invoke(contextRunner);
            
            logHandle?.Dispose();
            contextRunner.Dispose();

            resetEvent.WaitOne(TimeSpan.FromSeconds(5));

            return completedContext;
        }
        
        public (IActionContext, T) Create<T>(Func<IContextRunner, T> test)
            where T : class
        {
            var resetEvent = new AutoResetEvent(false);
            
            IDisposable logHandle = null;
            IActionContext completedContext = null;

            var contextRunner = new ActionContextRunner(c =>
                {
                    logHandle = c.Logger.WhenEntryLogged.Subscribe(
                        entry => { },
                        error => {
                            completedContext = c;

                            resetEvent.Set();
                        },
                        () =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        }
                    );
                }, _settings, _sanitizers);
            
            var result = test.Invoke(contextRunner);
            
            logHandle?.Dispose();
            contextRunner.Dispose();

            resetEvent.WaitOne(TimeSpan.FromSeconds(5));

            return (completedContext, result);
        }
        
        public async Task<IActionContext> CreateAsync(Func<IContextRunner, Task> test)
        {
            var resetEvent = new AutoResetEvent(false);
            
            IDisposable logHandle = null;
            IActionContext completedContext = null;

            var contextRunner = new ActionContextRunner(c =>
                {
                    logHandle = c.Logger.WhenEntryLogged.Subscribe(
                        entry => { },
                        error =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        },
                        () =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        }
                    );
                }, _settings, _sanitizers);
            
            await test.Invoke(contextRunner);
            
            logHandle?.Dispose();
            contextRunner.Dispose();

            resetEvent.WaitOne(TimeSpan.FromSeconds(5));

            return completedContext;
        }
        
        public async Task<(IActionContext, T)> CreateAsync<T>(Func<IContextRunner, Task<T>> test)
            where T : class
        {
            var resetEvent = new AutoResetEvent(false);
            
            IDisposable logHandle = null;
            IActionContext completedContext = null;

            var contextRunner = new ActionContextRunner(c =>
                {
                    logHandle = c.Logger.WhenEntryLogged.Subscribe(
                        entry => { },
                        error =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        },
                        () =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        }
                    );
                }, _settings, _sanitizers);
            
            var result = await test.Invoke(contextRunner);
            
            logHandle?.Dispose();
            contextRunner.Dispose();

            resetEvent.WaitOne(TimeSpan.FromSeconds(5));

            return (completedContext, result);
        }
    }
}