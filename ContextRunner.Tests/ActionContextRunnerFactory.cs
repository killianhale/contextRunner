using System;
using System.Threading;
using System.Threading.Tasks;
using ContextRunner.Base;

namespace ContextRunner.Tests
{
    public class ActionContextRunnerFactory
    {
        private readonly ActionContextSettings _settings;

        public ActionContextRunnerFactory(ActionContextSettings settings = null)
        {
            _settings = settings ?? new ActionContextSettings()
            {
                EnableContextEndMessage = false
            };
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
                        error => { },
                        () =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        }
                    );
                }, _settings);
            
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
                        error => { },
                        () =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        }
                    );
                }, _settings);
            
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
                        error => { },
                        () =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        }
                    );
                }, _settings);
            
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
                        error => { },
                        () =>
                        {
                            completedContext = c;

                            resetEvent.Set();
                        }
                    );
                }, _settings);
            
            var result = await test.Invoke(contextRunner);
            
            logHandle?.Dispose();
            contextRunner.Dispose();

            resetEvent.WaitOne(TimeSpan.FromSeconds(5));

            return (completedContext, result);
        }
    }
}