using System;
using ContextRunner.Logging;
using ContextRunner.State;

namespace ContextRunner.Base
{
    public interface IActionContext
    {
        ActionContextSettings Settings { get; }
        IContextLogger Logger { get; }
        IContextState State { get; }
        int Depth { get; }
        string ContextName { get; }
        string ContextGroupName { get; }
        bool IsRoot { get; }
        TimeSpan TimeElapsed { get; }
        bool ShouldSuppress();
        void Dispose();
    }
}