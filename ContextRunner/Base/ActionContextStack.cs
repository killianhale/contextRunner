using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ContextRunner.Logging;
using ContextRunner.State;

namespace ContextRunner.Base
{
    public class ActionContextStack
    {
        private readonly ConcurrentStack<IActionContext> _contexts;
        private readonly ConcurrentQueue<ContextSummary> _checkpoints;

        public ActionContextStack()
        {
            _contexts = new ConcurrentStack<IActionContext>();
            _checkpoints = new ConcurrentQueue<ContextSummary>();

            CorrelationId = Guid.NewGuid();
        }

        public Guid CorrelationId { get; }
        
        public bool IsEmpty => _contexts.IsEmpty;
        public bool ContainsOnlyRoot => _contexts.Count == 1;

        public void Push(IActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            _contexts.Push(context);
        }

        public IActionContext Pop()
        {
            _contexts.TryPop(out var result);

            return result;
        }

        public IActionContext Peek()
        {
            _contexts.TryPeek(out var result);

            return result;
        }

        public void CreateCheckpoint(string name, IActionContext context)
        {
            context.Info.Checkpoint = name;
            
            var summary = ContextSummary.CreateFromContext(context);
            summary.Data["contextInfo"] = context.Info;
            
            _checkpoints.Enqueue(summary);
        }

        public List<ContextSummary> GetCheckpoints()
        {
            return _checkpoints.ToList();
        }
    }
}