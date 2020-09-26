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
    public delegate void ContextLoadedHandler(IActionContext context);
    public delegate void ContextUnloadedHandler(IActionContext context);

    public class ActionContext : IActionContext
    {
        private static readonly AsyncLocal<ConcurrentDictionary<string, ActionContextStack>> _asyncLocalStacks = new AsyncLocal<ConcurrentDictionary<string, ActionContextStack>>();

        public static event ContextLoadedHandler Loaded;
        public static event ContextUnloadedHandler Unloaded;

        private readonly Stopwatch _stopwatch;
        private readonly IActionContext _parent;

        private readonly ConcurrentDictionary<string, ActionContextStack> _namedStacks;
        private readonly ActionContextStack _stack;


        public ActionContext(
            [CallerMemberName] string name = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> logSanitizers = null)
        : this("default", name, settings, logSanitizers)
        {}

        public ActionContext(
            string contextGroupName = "default",
            [CallerMemberName]string name = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> logSanitizers = null)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            
            _asyncLocalStacks.Value ??= new ConcurrentDictionary<string, ActionContextStack>();
            _namedStacks = _asyncLocalStacks.Value;
            _stack = _namedStacks.GetOrAdd(contextGroupName, new ActionContextStack());

            ContextName = name;
            ContextGroupName = contextGroupName;

            _parent = _stack.Peek();
            _stack.Push(this);
            
            Id = Guid.NewGuid();
            CausationId = _parent?.Id ?? Id;
            CorrelationId = _stack.CorrelationId;
            
            if(IsRoot)
            {
                Settings = settings ?? new ActionContextSettings();

                Depth = 0;
                State = new ContextState(logSanitizers);
                Logger = new ContextLogger(this);
            }
            else
            {
                Settings = _parent.Settings;

                Depth = _parent.Depth + 1;
                State = _parent.State;

                Logger = _parent.Logger;

                Logger.TrySetContext(this);
            }

            var shouldSuppressStartMessage = !IsRoot && Settings.SuppressChildContextStartMessages;
            var shouldAlwaysShowStart = Settings.EnableContextStartMessage && !shouldSuppressStartMessage;

            Logger.Log(Settings.ContextStartMessageLevel,
                $"Context {ContextName} has started.", !shouldAlwaysShowStart);

            Loaded?.Invoke(this);
        }

        public ActionContextSettings Settings { get; }
        public IContextLogger Logger { get; private set; }
        public IContextState State { get; private set; }
        
        public Action<IActionContext> OnDispose { get; set; }

        public int Depth { get; }
        public string ContextName { get; }
        public string ContextGroupName { get; }
        
        public Guid Id { get; }
        public Guid CorrelationId { get; }
        public Guid CausationId { get; }

        public bool IsRoot => _parent == null;

        public TimeSpan TimeElapsed => _stopwatch.Elapsed;

        public bool ShouldSuppress()
        {
            var isInSupressList = Settings.SuppressContextByNameList.Contains(ContextName);

            var levelOfEntry = (int)Logger.GetHighestLogLevel();
            var levelToNotify = (int)Settings.SuppressContextsByNameUnderLevel;

            var entryIsUnderNotifyLevel = levelOfEntry < levelToNotify;

            return isInSupressList && entryIsUnderNotifyLevel;
        }

        public void Dispose()
        {
            _stopwatch.Stop();

            var shouldSuppressEndMessage = !IsRoot && Settings.SuppressChildContextEndMessages;

            var shouldAlwaysShowEnd = Settings.EnableContextEndMessage && !shouldSuppressEndMessage;
            
                Logger.Log(Settings.ContextEndMessageLevel,
                    $"Context {ContextName} has ended.", !shouldAlwaysShowEnd);

            _stack.Pop();

            Logger.CompleteIfRoot();
            Logger.TrySetContext(_parent);

            if (_stack.IsEmpty)
            {
                // Logger = null;
                // State = null;

                _namedStacks.Remove(ContextGroupName, out _);

                if (!_namedStacks.Any())
                {
                    _asyncLocalStacks.Value = null;
                }
            }

            OnDispose?.Invoke(this);
            Unloaded?.Invoke(this);
        }
    }
}
