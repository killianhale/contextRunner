using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ContextRunner.Base
{
    public class ActionContextStack
    {
        private readonly ConcurrentStack<IActionContext> _namedContexts;

        public ActionContextStack()
        {
            _namedContexts = new ConcurrentStack<IActionContext>();

            CorrelationId = Guid.NewGuid();
        }

        public Guid CorrelationId { get; }
        
        public bool IsEmpty => _namedContexts.IsEmpty;
        public bool ContainsOnlyRoot => _namedContexts.Count == 1;

        public void Push(IActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            _namedContexts.Push(context);
        }

        public IActionContext Pop()
        {
            _namedContexts.TryPop(out var result);

            return result;
        }

        public IActionContext Peek()
        {
            _namedContexts.TryPeek(out var result);

            return result;
        }
    }
}