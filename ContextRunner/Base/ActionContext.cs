using System.Diagnostics;
using System.Runtime.CompilerServices;
using ContextRunner.Logging;
using ContextRunner.State;

namespace ContextRunner.Base
{
    public delegate void ContextLoadedHandler(IActionContext context);
    public delegate void ContextUnloadedHandler(IActionContext context);

    public class ActionContext : IActionContext
    {
        private static readonly AsyncLocal<ConcurrentDictionary<string, ActionContextStack>?> AsyncLocalStacks = new ();

        public static event ContextLoadedHandler? Loaded;
        public static event ContextUnloadedHandler? Unloaded;

        private readonly Stopwatch _stopwatch;
        private readonly IActionContext? _parent;

        private readonly ConcurrentDictionary<string, ActionContextStack> _namedStacks;
        private readonly ActionContextStack _stack;


        public ActionContext(
            [CallerMemberName] string? name = null,
            ActionContextSettings? settings = null,
            IEnumerable<ISanitizer>?logSanitizers = null)
            : this("default", name, settings, logSanitizers)
        {
        }

        public ActionContext(
            string contextGroupName = "default",
            [CallerMemberName] string? name = null,
            ActionContextSettings? settings = null,
            IEnumerable<ISanitizer>? logSanitizers = null)
        {
            logSanitizers = logSanitizers?.ToList() ?? [];
            
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            AsyncLocalStacks.Value ??= new ConcurrentDictionary<string, ActionContextStack>();
            _namedStacks = AsyncLocalStacks.Value;
            _stack = _namedStacks.GetOrAdd(contextGroupName, new ActionContextStack());

            _parent = _stack.Peek();
            _stack.Push(this);

            var id = Guid.NewGuid();
            // var causationId = _parent?.Info.Id ?? id;
            // var correlationId = _stack.CorrelationId;

            if (_parent == null)
            {
                Settings = settings ?? new ActionContextSettings();
                
                Info = new ContextInfo(
                    true,
                    0,
                    name ?? $"Context_{id}",
                    contextGroupName,
                    _stack.CorrelationId);
                
                State = new ContextState(logSanitizers);
                Logger = new ContextLogger(this);
            }
            else
            {
                Settings = _parent.Settings;
                
                Info = new ContextInfo(
                    false,
                    _parent.Info.Depth + 1,
                    name ?? $"Context_{id}",
                    contextGroupName,
                    _stack.CorrelationId,
                    _parent.Info.Id);
                
                State = _parent.State;
                Logger = _parent.Logger;

                Logger.TrySetContext(this);
            }

            var entryType = Info.IsRoot ? ContextLogEntryType.ContextStart : ContextLogEntryType.ChildContextStart;
            Logger.LogAsType(Settings.ContextStartMessageLevel,
                $"Context {Info.ContextName} has started.", entryType);

            Loaded?.Invoke(this);
        }

        public ActionContextSettings Settings { get; }
        public IContextLogger Logger { get; }
        public IContextState State { get; }

        public IContextInfo Info { get; }

        public Action<IActionContext>? OnDispose { get; set; }

        public TimeSpan TimeElapsed => _stopwatch.Elapsed;

        public void CreateCheckpoint(string name)
        {
            _stack.CreateCheckpoint(name, this);
            
            State.Clear();
            Logger.LogEntries.Clear();
        }
        
        public List<ContextSummary> GetCheckpoints()
        {
            return _stack.GetCheckpoints();
        }

        public bool ShouldSuppress()
        {
            var isInSuppressList = Settings.SuppressContextByNameList.Contains(Info.ContextName);

            var levelOfEntry = (int) Logger.GetHighestLogLevel();
            var levelToNotify = (int) Settings.SuppressContextsByNameUnderLevel;

            var entryIsUnderNotifyLevel = levelOfEntry < levelToNotify;

            return isInSuppressList && entryIsUnderNotifyLevel;
        }

        public void Dispose()
        {
            _stopwatch.Stop();

            var entryType = Info.IsRoot ? ContextLogEntryType.ContextEnd : ContextLogEntryType.ChildContextEnd;
            Logger.LogAsType(Settings.ContextEndMessageLevel,
                $"Context {Info.ContextName} has ended.", entryType);

            _stack.Pop();

            Logger.CompleteIfRoot();

            if (_parent != null)
            {
                Logger.TrySetContext(_parent);
            }

            if (_stack.IsEmpty)
            {
                // Logger = null;
                // State = null;

                _namedStacks.Remove(Info.ContextGroupName, out _);

                if (_namedStacks.IsEmpty)
                {
                    AsyncLocalStacks.Value = null;
                }
            }

            GC.SuppressFinalize(this);
            OnDispose?.Invoke(this);
            Unloaded?.Invoke(this);
        }
    }
}
