using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ContextRunner.Base;
using Xunit;

namespace ContextRunner.Tests
{
    public class ActionContextRunnerTests
    {
        [Fact]
        public void ConcurrentNamedContextsExecuteSuccessfully()
        {
            var contextNames = new[] {"One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"};
            
            var completedContexts = new ConcurrentQueue<IActionContext>();

            var result = Parallel.ForEach(contextNames, new ParallelOptions() { MaxDegreeOfParallelism = 10} ,name =>
            {
                IDisposable logHandle = null;
                
                var contextRunner = new ActionContextRunner(c =>
                {
                    logHandle = c.Logger.WhenEntryLogged.Subscribe(
                        _ => { },
                        () => completedContexts.Enqueue(c));
                });

                contextRunner.CreateAndAppendToActionExceptions(context =>
                {
                    context.Logger.Information("This is a test!");
                    
                    
                    contextRunner.CreateAndAppendToActionExceptions(nestedContext =>
                    {
                        nestedContext.Logger.Information("This is a test!");
                    }, name);
                }, "Run", "Test");
                
                logHandle?.Dispose();
                contextRunner.Dispose();
            });

            while (!result.IsCompleted)
            {
                Thread.Sleep(10);
            }
        }
        
        [Fact]
        public void LogsOutputSuccessfullyWhenUsingCreate()
        {
            var completedContexts = new ConcurrentQueue<IActionContext>();

            IDisposable logHandle = null;
            
            var contextRunner = new ActionContextRunner(c =>
            {
                logHandle = c.Logger.WhenEntryLogged.Subscribe(
                    _ => { },
                    () => completedContexts.Enqueue(c));
            });

            using(var context = contextRunner.Create("Test"))
            {
                context.Logger.Information("This is a test!");
            }
            
            logHandle?.Dispose();
            contextRunner.Dispose();
            
            Assert.Single(completedContexts);
        }
    }
}
