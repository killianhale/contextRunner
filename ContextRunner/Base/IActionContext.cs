using System;
using ContextRunner.Logging;
using ContextRunner.State;

namespace ContextRunner.Base
{
    public interface IActionContext : IDisposable
    {
        ActionContextSettings Settings { get; }
        IContextLogger Logger { get; }
        IContextState State { get; }
        int Depth { get; }
        string ContextName { get; }
        string ContextGroupName { get; }
        Guid Id { get; }
        Guid CorrelationId { get; }
        Guid CausationId { get; }
        bool IsRoot { get; }
        TimeSpan TimeElapsed { get; }
        bool ShouldSuppress();
        void Dispose();
    }
}