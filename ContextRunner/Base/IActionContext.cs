using System;
using System.Collections.Generic;
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
        
        void CreateCheckpoint(string name);
        List<ContextSummary> GetCheckpoints();
        
        bool ShouldSuppress();
    }
}