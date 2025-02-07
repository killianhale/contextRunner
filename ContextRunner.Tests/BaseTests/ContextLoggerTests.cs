using System.Linq;
using System.Threading;
using ContextRunner.Base;
using ContextRunner.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

// ReSharper disable ExplicitCallerInfoArgument

namespace ContextRunner.Tests.BaseTests
{
    public class ContextLoggerTests
    {
        [Theory]
        [InlineData(LogLevel.None)]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public void ContextLoggerLogOutputsLogLevel(LogLevel level)
        {
            const string message = "This is a test";
            
            using var context = new ActionContext("default", "TestContext");
            Thread.Sleep(10);
            
            var logger = new ContextLogger(context);
            logger.Log(level, message);

            var entry = logger.LogEntries.FirstOrDefault();
            
            Assert.NotNull(entry);
            Assert.NotNull(logger.LogEntries);
            Assert.Single(logger.LogEntries);
            Assert.Equal(level, entry.LogLevel);
            Assert.Equal(message, entry.Message);
            Assert.Equal(context.Info.ContextName, entry.ContextName);
            Assert.Equal(context.Info.Id, entry.ContextId);
            Assert.True(entry.TimeElapsed.TotalMilliseconds > 10);
        }

        [Fact]
        public void ContextLoggerTraceOutputsSuccessfully()
        {
            const string message = "This is a test";
            
            using var context = new ActionContext("default", "TestContext");
            Thread.Sleep(10);

            var logger = new ContextLogger(context);
            logger.Trace(message);

            var entry = logger.LogEntries.FirstOrDefault();
            
            Assert.NotNull(entry);
            Assert.NotNull(logger.LogEntries);
            Assert.Single(logger.LogEntries);
            Assert.Equal(LogLevel.Trace, entry.LogLevel);
            Assert.Equal(message, entry.Message);
            Assert.Equal(context.Info.ContextName, entry.ContextName);
            Assert.Equal(context.Info.Id, entry.ContextId);
            Assert.True(entry.TimeElapsed.TotalMilliseconds > 10);
        }

        [Fact]
        public void ContextLoggerDebugOutputsSuccessfully()
        {
            const string message = "This is a test";
            
            using var context = new ActionContext("default", "TestContext");
            Thread.Sleep(10);

            var logger = new ContextLogger(context);
            logger.Debug(message);

            var entry = logger.LogEntries.FirstOrDefault();
            
            Assert.NotNull(entry);
            Assert.NotNull(logger.LogEntries);
            Assert.Single(logger.LogEntries);
            Assert.Equal(LogLevel.Debug, entry.LogLevel);
            Assert.Equal(message, entry.Message);
            Assert.Equal(context.Info.ContextName, entry.ContextName);
            Assert.Equal(context.Info.Id, entry.ContextId);
            Assert.True(entry.TimeElapsed.TotalMilliseconds > 10);
        }

        [Fact]
        public void ContextLoggerInformationOutputsSuccessfully()
        {
            const string message = "This is a test";
            
            using var context = new ActionContext("default", "TestContext");
            Thread.Sleep(10);

            var logger = new ContextLogger(context);
            logger.Information(message);

            var entry = logger.LogEntries.FirstOrDefault();
            
            Assert.NotNull(entry);
            Assert.NotNull(logger.LogEntries);
            Assert.Single(logger.LogEntries);
            Assert.Equal(LogLevel.Information, entry.LogLevel);
            Assert.Equal(message, entry.Message);
            Assert.Equal(context.Info.ContextName, entry.ContextName);
            Assert.Equal(context.Info.Id, entry.ContextId);
            Assert.True(entry.TimeElapsed.TotalMilliseconds > 10);
        }

        [Fact]
        public void ContextLoggerWarningOutputsSuccessfully()
        {
            const string message = "This is a test";
            
            using var context = new ActionContext("default", "TestContext");
            Thread.Sleep(10);

            var logger = new ContextLogger(context);
            logger.Warning(message);

            var entry = logger.LogEntries?.FirstOrDefault();
            
            Assert.NotNull(entry);
            Assert.NotNull(logger.LogEntries);
            Assert.Single(logger.LogEntries);
            Assert.Equal(LogLevel.Warning, entry.LogLevel);
            Assert.Equal(message, entry.Message);
            Assert.Equal(context.Info.ContextName, entry.ContextName);
            Assert.Equal(context.Info.Id, entry.ContextId);
            Assert.True(entry.TimeElapsed.TotalMilliseconds > 10);
        }

        [Fact]
        public void ContextLoggerErrorOutputsSuccessfully()
        {
            const string message = "This is a test";
            
            using var context = new ActionContext("default", "TestContext");
            Thread.Sleep(10);

            var logger = new ContextLogger(context);
            logger.Error(message);

            var entry = logger.LogEntries?.FirstOrDefault();
            
            Assert.NotNull(entry);
            Assert.NotNull(logger.LogEntries);
            Assert.Single(logger.LogEntries);
            Assert.Equal(LogLevel.Error, entry.LogLevel);
            Assert.Equal(message, entry.Message);
            Assert.Equal(context.Info.ContextName, entry.ContextName);
            Assert.Equal(context.Info.Id, entry.ContextId);
            Assert.True(entry.TimeElapsed.TotalMilliseconds > 10);
        }

        [Fact]
        public void ContextLoggerCriticalOutputsSuccessfully()
        {
            const string message = "This is a test";

            using var context = new ActionContext("default", "TestContext");
            Thread.Sleep(10);

            var logger = new ContextLogger(context);
            logger.Critical(message);

            var entry = logger.LogEntries?.FirstOrDefault();
            
            Assert.NotNull(entry);
            Assert.NotNull(logger.LogEntries);
            Assert.Single(logger.LogEntries);
            Assert.Equal(LogLevel.Critical, entry.LogLevel);
            Assert.Equal(message, entry.Message);
            Assert.Equal(context.Info.ContextName, entry.ContextName);
            Assert.Equal(context.Info.Id, entry.ContextId);
            Assert.True(entry.TimeElapsed.TotalMilliseconds > 10);
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
        public void ContextLoggerOutputsHighestLogLevelCorrectly(LogLevel[] levels, LogLevel expected)
        {
            const string message = "This is a test";

            using var context = new ActionContext("default", "TestContext");

            var logger = new ContextLogger(context);

            foreach (var level in levels)
            {
                logger.Log(level, message);
            }
            
            Assert.NotNull(logger);
            Assert.Equal(levels.Length, logger.LogEntries.Count);
            Assert.Equal(expected, logger.GetHighestLogLevel());
        }

        [Theory]
        [InlineData(new[] { LogLevel.None }, LogLevel.None, "successfully")]
        [InlineData(new[] { LogLevel.None, LogLevel.Trace }, LogLevel.Trace, "successfully")]
        [InlineData(new[] { LogLevel.Trace }, LogLevel.Trace, "successfully")]
        [InlineData(new[] { LogLevel.None, LogLevel.Debug }, LogLevel.Debug, "successfully")]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Debug }, LogLevel.Debug, "successfully")]
        [InlineData(new[] { LogLevel.Debug }, LogLevel.Debug, "successfully")]
        [InlineData(new[] { LogLevel.None, LogLevel.Information }, LogLevel.Information, "successfully")]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Information }, LogLevel.Information, "successfully")]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Information }, LogLevel.Information, "successfully")]
        [InlineData(new[] { LogLevel.Information }, LogLevel.Information, "successfully")]
        [InlineData(new[] { LogLevel.None, LogLevel.Warning }, LogLevel.Warning, "with a warning")]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Warning }, LogLevel.Warning, "with a warning")]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Warning }, LogLevel.Warning, "with a warning")]
        [InlineData(new[] { LogLevel.Information, LogLevel.Warning }, LogLevel.Warning, "with a warning")]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Warning }, LogLevel.Warning, "with multiple warnings")]
        [InlineData(new[] { LogLevel.Warning }, LogLevel.Warning, "with a warning")]
        [InlineData(new[] { LogLevel.None, LogLevel.Error }, LogLevel.Error, "with an error")]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Error }, LogLevel.Error, "with an error")]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Error }, LogLevel.Error, "with an error")]
        [InlineData(new[] { LogLevel.Information, LogLevel.Error }, LogLevel.Error, "with an error")]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Error }, LogLevel.Error, "with an error")]
        [InlineData(new[] { LogLevel.Error, LogLevel.Error }, LogLevel.Error, "with multiple errors")]
        [InlineData(new[] { LogLevel.Error }, LogLevel.Error, "with an error")]
        [InlineData(new[] { LogLevel.None, LogLevel.Critical }, LogLevel.Critical, "with a critical error")]
        [InlineData(new[] { LogLevel.Trace, LogLevel.Critical }, LogLevel.Critical, "with a critical error")]
        [InlineData(new[] { LogLevel.Debug, LogLevel.Critical }, LogLevel.Critical, "with a critical error")]
        [InlineData(new[] { LogLevel.Information, LogLevel.Critical }, LogLevel.Critical, "with a critical error")]
        [InlineData(new[] { LogLevel.Warning, LogLevel.Critical }, LogLevel.Critical, "with a critical error")]
        [InlineData(new[] { LogLevel.Error, LogLevel.Critical }, LogLevel.Critical, "with a critical error")]
        [InlineData(new[] { LogLevel.Critical, LogLevel.Critical }, LogLevel.Critical, "with multiple critical errors")]
        [InlineData(new[] { LogLevel.Critical }, LogLevel.Critical, "with a critical error")]
        public void ContextLoggerOutputsSummaryCorrectly(LogLevel[] levels, LogLevel expectedLevel, string expectedMessage)
        {
            const string message = "This is a test";

            using var context = new ActionContext("default", "TestContext");
            Thread.Sleep(10);

            var logger = new ContextLogger(context);

            foreach (var level in levels)
            {
                logger.Log(level, message);
            }

            var summary = logger.GetSummaryLogEntry();
            
            Assert.NotNull(logger);
            Assert.Equal(levels.Length, logger.LogEntries.Count);
            Assert.Equal(context.Info.ContextName, summary.ContextName);
            Assert.Equal(context.Info.Id, summary.ContextId);
            Assert.Equal(expectedLevel, summary.LogLevel);
            Assert.StartsWith($"The context '{context.Info.ContextName}' ended {expectedMessage}", summary.Message);
            Assert.True(summary.TimeElapsed.TotalMilliseconds > 10);
        }

        [Fact]
        public void ContextLoggerCanSetContext()
        {
            const string message1 = "This is a test 1";
            const string message2 = "This is a test 2";

            using var context1 = new ActionContext("default", "TestContext1");
            using var context2 = new ActionContext("default", "TestContext2");

            var logger = new ContextLogger(context1);
            logger.Trace(message1);
            logger.TrySetContext(context2);
            logger.Trace(message2);
            
            Assert.NotNull(logger.LogEntries);
            Assert.Equal(2, logger.LogEntries.Count);

            var entries = logger.LogEntries.ToList();
            var entry1 = entries[0];
            var entry2 = entries[1];
            
            Assert.Equal(LogLevel.Trace, entry1.LogLevel);
            Assert.Equal(message1, entry1.Message);
            Assert.Equal(context1.Info.ContextName, entry1.ContextName);
            Assert.Equal(context1.Info.Id, entry1.ContextId);
            
            Assert.Equal(LogLevel.Trace, entry2.LogLevel);
            Assert.Equal(message2, entry2.Message);
            Assert.Equal(context2.Info.ContextName, entry2.ContextName);
            Assert.Equal(context2.Info.Id, entry2.ContextId);
        }
    }
}
