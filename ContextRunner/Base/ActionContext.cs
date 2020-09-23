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
        private static readonly AsyncLocal<Guid?> _correlationId = new AsyncLocal<Guid?>();
        private static readonly ConcurrentDictionary<string, AsyncLocal<IActionContext>> _namedContexts = new ConcurrentDictionary<string, AsyncLocal<IActionContext>>();

        public static event ContextLoadedHandler Loaded;
        public static event ContextUnloadedHandler Unloaded;

        private readonly Stopwatch _stopwatch;
        private readonly IActionContext _parent;


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

            ContextName = name;
            ContextGroupName = contextGroupName;

            _correlationId.Value ??= Guid.NewGuid();
            var baseId = _correlationId.Value;

            var groupId = $"{ContextGroupName}_{baseId.Value}";
            _parent = _namedContexts.GetOrAdd(groupId, new AsyncLocal<IActionContext>()).Value;
            _namedContexts[groupId].Value = this;
            
            if(IsRoot)
            {
                Settings = settings ?? new ActionContextSettings();

                Depth = 0;
                State = new ContextState(logSanitizers);
                Logger = new ContextLogger(this);
            
                Id = baseId.Value;
                CausationId = Id;
                CorrelationId = Id;
            }
            else
            {
                Settings = _parent.Settings;

                Depth = _parent.Depth + 1;
                State = _parent.State;

                Logger = _parent.Logger;
            
                Id = Guid.NewGuid();
                CausationId = _parent?.Id ?? Id;
                CorrelationId = _parent?.CorrelationId ?? Id;

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

        public bool IsRoot {
            get => _parent == null;
        }

        public TimeSpan TimeElapsed
        {
            get => _stopwatch.Elapsed;
        }

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

            var groupId = $"{ContextGroupName}_{CorrelationId}";
            _namedContexts[groupId].Value = _parent;

            Logger.CompleteIfRoot();
            Logger.TrySetContext(_parent);

            if (IsRoot)
            {
                Logger = null;
                State = null;

                _correlationId.Value = null;
                
                _namedContexts.Remove(groupId, out _);
            }

            OnDispose?.Invoke(this);
            Unloaded?.Invoke(this);
        }
    }
}
