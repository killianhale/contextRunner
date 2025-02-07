namespace ContextRunner.Base
{
    public class ActionContextStack
    {
        private readonly ConcurrentStack<IActionContext> _contexts = new();
        private readonly ConcurrentQueue<ContextSummary> _checkpoints = new();

        public Guid CorrelationId { get; } = Guid.NewGuid();

        public bool IsEmpty => _contexts.IsEmpty;
        public bool ContainsOnlyRoot => _contexts.Count == 1;

        public void Push(IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _contexts.Push(context);
        }

        public IActionContext? Pop()
        {
            _contexts.TryPop(out var result);

            return result;
        }

        public IActionContext? Peek()
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