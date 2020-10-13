using System;

namespace ContextRunner.Base
{
    public interface IContextInfo
    {
        bool IsRoot { get; }
        int Depth { get; }
        string ContextName { get; }
        string ContextGroupName { get; }
        Guid Id { get; }
        Guid CorrelationId { get; }
        Guid CausationId { get; }
    }
}