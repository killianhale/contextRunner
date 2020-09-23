using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContextRunner.Base;
using Xunit;

namespace ContextRunner.Tests
{
    public class ActionContextRunnerTests
    {
        [Fact]
        public void ConcurrentChildContextsExecuteSuccessfully()
        {
            var contextNames = new List<string>();

            for (var x = 0; x < 200; x++)
            {
                contextNames.Add($"Test_{Guid.NewGuid()}");
            }
            
            var completedContexts = new ConcurrentQueue<IActionContext>();

            var result = Parallel.ForEach(contextNames, new ParallelOptions() { MaxDegreeOfParallelism = 20} ,name =>
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
                }, "Run");
                
                logHandle?.Dispose();
                contextRunner.Dispose();
            });

            while (!result.IsCompleted)
            {
                Thread.Sleep(10);
            }
            
            Assert.Equal(contextNames.Count, completedContexts.Count);
        }
        
        [Fact]
        public void ConcurrentNamedContextsExecuteSuccessfully()
        {
            var contextNames = new List<string>();

            for (var x = 0; x < 200; x++)
            {
                contextNames.Add($"Test_{Guid.NewGuid()}");
            }
            
            var completedContexts = new ConcurrentQueue<IActionContext>();

            var result = Parallel.ForEach(contextNames, new ParallelOptions() { MaxDegreeOfParallelism = 20} ,name =>
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
            
            Assert.Equal(contextNames.Count * 2, completedContexts.Count);
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
