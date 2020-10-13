using System;

namespace ContextRunner.Base
{
    public interface IRootContextInfo
    {
        string ContextName { get; }
        string ContextGroupName { get; }
        public string Checkpoint { get; set; }
        Guid Id { get; }
    }
    
    public interface IContextInfo : IRootContextInfo
    {
        bool IsRoot { get; }
        int Depth { get; }
        Guid CorrelationId { get; }
        Guid CausationId { get; }
    }
}