using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContextRunner.Base;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContextRunner.Tests
{
    public class ActionContextRunnerTests
    {
        [Fact]
        public void SyncActionContextActionIsCalled()
        {
            var factory = new ActionContextRunnerFactory();
            
            var spy = new Mock<object>();
            spy.Setup(o => o.ToString())
                .Returns("Test");

            factory.Create( contextRunner =>
            {
                contextRunner.CreateAndAppendToActionExceptions(context =>
                {
                    context.Logger.Information("This is a test!");

                    var _ = spy.Object.ToString();
                }, "Test");
            });
            
            spy.Verify(mock => mock.ToString(), Times.Once);
        }

        [Fact]
        public void SyncFuncContextActionIsCalled()
        {
            var factory = new ActionContextRunnerFactory();

            var spy = new Mock<object>();
            spy.Setup(o => o.ToString())
                .Returns("Test");
            
            var (_, result) = factory.Create<object>( contextRunner =>
            {
                return contextRunner.CreateAndAppendToActionExceptions(context =>
                {
                    context.Logger.Information("This is a test!");

                    return spy.Object.ToString();
                }, "Test");
            });
            
            spy.Verify(mock => mock.ToString(), Times.Once);
            Assert.Equal("Test", result);
        }
        
        [Fact]
        public async Task AsyncActionContextActionIsCalled()
        {
            var factory = new ActionContextRunnerFactory();

            var spy = new Mock<object>();
            spy.Setup(o => o.ToString())
                .Returns("Test");

            await factory.CreateAsync(async contextRunner =>
            {
                await contextRunner.CreateAndAppendToActionExceptionsAsync(async context =>
                {
                    context.Logger.Information("This is a test!");

                    await Task.Delay(5);

                    var _ = spy.Object.ToString();
                }, "Test");
            });
            
            spy.Verify(mock => mock.ToString(), Times.Once);
        }
        
        [Fact]
        public async Task AsyncFuncContextActionIsCalled()
        {
            var factory = new ActionContextRunnerFactory();

            var spy = new Mock<object>();
            spy.Setup(o => o.ToString())
                .Returns("Test");

            var (_, result) = await factory.CreateAsync<object>(async contextRunner =>
            {
                return await contextRunner.CreateAndAppendToActionExceptionsAsync(async context =>
                {
                    context.Logger.Information("This is a test!");

                    await Task.Delay(5);

                    return spy.Object.ToString();
                }, "Test");
            });
            
            spy.Verify(mock => mock.ToString(), Times.Once);
            Assert.Equal("Test", result);
        }

        [Theory]
        [InlineData(LogLevel.None)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public void SyncActionContextOutputsLogLevel(LogLevel level)
        {
            var factory = new ActionContextRunnerFactory();

            var completedContext = factory.Create(contextRunner =>
            {
                contextRunner.CreateAndAppendToActionExceptions(
                    context => { context.Logger.Log(level, "This is a test"); }, "Test");
            });
            
            Assert.NotNull(completedContext);
            Assert.Equal(1, completedContext?.Logger?.LogEntries.Count);
            Assert.Equal(level, completedContext.Logger?.LogEntries?.FirstOrDefault()?.LogLevel);
        }

        [Theory]
        [InlineData(LogLevel.None)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public void SyncFuncContextOutputsLogLevel(LogLevel level)
        {
            var factory = new ActionContextRunnerFactory();

            var (completedContext, _) = factory.Create<object>(contextRunner =>
            {
                return contextRunner.CreateAndAppendToActionExceptions<object>(
                    context =>
                    {
                        context.Logger.Log(level, "This is a test");

                        return null;
                    }, "Test");
            });
            
            Assert.NotNull(completedContext);
            Assert.Equal(1, completedContext?.Logger?.LogEntries.Count);
            Assert.Equal(level, completedContext.Logger?.LogEntries?.FirstOrDefault()?.LogLevel);
        }

        [Theory]
        [InlineData(LogLevel.None)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public async Task AsyncActionContextOutputsLogLevel(LogLevel level)
        {
            var factory = new ActionContextRunnerFactory();

            var completedContext = await factory.CreateAsync(async contextRunner =>
            {
                await contextRunner.CreateAndAppendToActionExceptionsAsync(async context =>
                {
                    context.Logger.Log(level, "This is a test");

                    await Task.Delay(5);
                }, "Test");
            });
            
            Assert.NotNull(completedContext);
            Assert.Equal(1, completedContext?.Logger?.LogEntries.Count);
            Assert.Equal(level, completedContext.Logger?.LogEntries?.FirstOrDefault()?.LogLevel);
        }

        [Theory]
        [InlineData(LogLevel.None)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public async Task AsyncFuncContextOutputsLogLevel(LogLevel level)
        {
            var factory = new ActionContextRunnerFactory();

            var (completedContext, result) = await factory.CreateAsync<object>(async contextRunner =>
            {
                return await contextRunner.CreateAndAppendToActionExceptionsAsync<object>(async context =>
                {
                    context.Logger.Log(level, "This is a test");

                    await Task.Delay(5);

                    return null;
                }, "Test");
            });
            
            Assert.NotNull(completedContext);
            Assert.Equal(1, completedContext?.Logger?.LogEntries.Count);
            Assert.Equal(level, completedContext.Logger?.LogEntries?.FirstOrDefault()?.LogLevel);
        }

        [Theory]
        [InlineData(new[] { LogLevel.None }, LogLevel.None)]
        [InlineData(new[] { LogLevel.None, LogLevel.Trace }, LogLevel.Trace)]
        [InlineData(new[] { LogLevel.Trace }, LogLevel.Trace)]
        [InlineData(new[] { LogLevel.None, LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.None, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.None, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.None, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.None, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Error, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Critical }, LogLevel.Critical)]
        public void SyncActionContextOutputsHighestLogLevelInSummary(LogLevel[] levels, LogLevel expected)
        {
            var factory = new ActionContextRunnerFactory();

            var completedContext = factory.Create(contextRunner =>
            {
                contextRunner.CreateAndAppendToActionExceptions(
                    context =>
                    {
                        foreach (var level in levels)
                        {
                            context.Logger.Log(level, "This is a test");
                        }
                    }, "Test");
            });
            
            Assert.NotNull(completedContext);
            Assert.Equal(levels.Length, completedContext?.Logger?.LogEntries.Count);
            Assert.Equal(expected, completedContext.Logger?.GetHighestLogLevel());
        }

        [Theory]
        [InlineData(new[] { LogLevel.None }, LogLevel.None)]
        [InlineData(new[] { LogLevel.None, LogLevel.Trace }, LogLevel.Trace)]
        [InlineData(new[] { LogLevel.Trace }, LogLevel.Trace)]
        [InlineData(new[] { LogLevel.None, LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.None, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.None, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.None, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.None, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Error, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Critical }, LogLevel.Critical)]
        public void SyncFuncContextOutputsHighestLogLevelInSummary(LogLevel[] levels, LogLevel expected)
        {
            var factory = new ActionContextRunnerFactory();

            var (completedContext, _) = factory.Create<object>(contextRunner =>
            {
                return contextRunner.CreateAndAppendToActionExceptions<object>(
                    context =>
                    {
                        foreach (var level in levels)
                        {
                            context.Logger.Log(level, "This is a test");
                        }

                        return null;
                    }, "Test");
            });
            
            Assert.NotNull(completedContext);
            Assert.Equal(levels.Length, completedContext?.Logger?.LogEntries.Count);
            Assert.Equal(expected, completedContext.Logger?.GetHighestLogLevel());
        }

        [Theory]
        [InlineData(new[] { LogLevel.None }, LogLevel.None)]
        [InlineData(new[] { LogLevel.None, LogLevel.Trace }, LogLevel.Trace)]
        [InlineData(new[] { LogLevel.Trace }, LogLevel.Trace)]
        [InlineData(new[] { LogLevel.None, LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.None, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.None, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.None, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.None, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Error, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Critical }, LogLevel.Critical)]
        public async Task AsyncActionContextOutputsHighestLogLevelInSummary(LogLevel[] levels, LogLevel expected)
        {
            var factory = new ActionContextRunnerFactory();

            var completedContext = await factory.CreateAsync(async contextRunner =>
            {
                await contextRunner.CreateAndAppendToActionExceptionsAsync(async context =>
                    {
                        foreach (var level in levels)
                        {
                            context.Logger.Log(level, "This is a test");
                        }

                        await Task.Delay(5);
                    }, "Test");
            });
            
            Assert.NotNull(completedContext);
            Assert.Equal(levels.Length, completedContext?.Logger?.LogEntries.Count);
            Assert.Equal(expected, completedContext.Logger?.GetHighestLogLevel());
        }

        [Theory]
        [InlineData(new[] { LogLevel.None }, LogLevel.None)]
        [InlineData(new[] { LogLevel.None, LogLevel.Trace }, LogLevel.Trace)]
        [InlineData(new[] { LogLevel.Trace }, LogLevel.Trace)]
        [InlineData(new[] { LogLevel.None, LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.Debug }, LogLevel.Debug)]
        [InlineData(new[] { LogLevel.None, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.Information }, LogLevel.Information)]
        [InlineData(new[] { LogLevel.None, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.Warning }, LogLevel.Warning)]
        [InlineData(new[] { LogLevel.None, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.Error }, LogLevel.Error)]
        [InlineData(new[] { LogLevel.None, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Information, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Error, LogLevel.Critical }, LogLevel.Critical)]
        [InlineData(new[] { LogLevel.Critical }, LogLevel.Critical)]
        public async Task AsyncFuncContextOutputsHighestLogLevelInSummary(LogLevel[] levels, LogLevel expected)
        {
            var factory = new ActionContextRunnerFactory();

            var (completedContext, _) = await factory.CreateAsync<object>(async contextRunner =>
            {
                return await contextRunner.CreateAndAppendToActionExceptionsAsync<object>(async context =>
                    {
                        foreach (var level in levels)
                        {
                            context.Logger.Log(level, "This is a test");
                        }

                        await Task.Delay(5);

                        return null;
                    }, "Test");
            });
            
            Assert.NotNull(completedContext);
            Assert.Equal(levels.Length, completedContext?.Logger?.LogEntries.Count);
            Assert.Equal(expected, completedContext.Logger?.GetHighestLogLevel());
        }

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
