using System;
using ContextRunner.Logging;
using ContextRunner.State;

namespace ContextRunner.Base
{
    public interface IActionContext : IDisposable
    {
        ActionContextSettings Settings { get; }
        
        IContextInfo Info { get; }
        IContextLogger Logger { get; }
        IContextState State { get; }
        
        TimeSpan TimeElapsed { get; }
        bool ShouldSuppress();
    }
}