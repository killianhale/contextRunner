using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using ContextRunner.Logging;
using ContextRunner.State;
using System.Collections.Generic;
using System.Linq;

namespace ContextRunner.Base
{
    public class ContextInfo : IContextInfo
    {
        public ContextInfo(
            bool isRoot,
            int depth,
            string contextName,
            string contextGroupName,
            Guid? correlationId = null,
            Guid? causationId = null
            )
        {
            IsRoot = isRoot;
            Depth = depth;
            ContextName = contextName;
            ContextGroupName = contextGroupName;
            
            Id = Guid.NewGuid();
            CausationId = causationId ?? Id;
            CorrelationId = correlationId ?? CausationId;
        }

        public bool IsRoot { get; }
        public int Depth { get; }
        
        public string ContextName { get; }
        public string ContextGroupName { get; }
        public string Checkpoint { get; set; }

        public Guid Id { get; }
        public Guid CorrelationId { get; }
        public Guid CausationId { get; }

    }
}
