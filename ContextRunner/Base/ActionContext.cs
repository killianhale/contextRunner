using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using ContextRunner.Logging;
using ContextRunner.State;
using System.Collections.Generic;
using System.Linq;

namespace ContextRunner.Base
{
    public delegate void ContextLoadedHandler(ActionContext context);
    public delegate void ContextUnloadedHandler(ActionContext context);

    public class ActionContext : IDisposable
    {
        private static readonly AsyncLocal<ActionContext> _current = new AsyncLocal<ActionContext>();

        public static event ContextLoadedHandler Loaded;
        public static event ContextUnloadedHandler Unloaded;

        private readonly Stopwatch _stopwatch;
        private readonly ActionContext _parent;

        public ActionContext(
            [CallerMemberName]string name = null,
            ActionContextSettings settings = null,
            IEnumerable<ISanitizer> logSanitizers = null)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            _parent = _current.Value;
            _current.Value = this;

            ContextName = name;
            
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
        public ContextLogger Logger { get; }
        public ContextState State { get; }

        public int Depth { get; }
        public string ContextName { get; }

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

            _current.Value = _parent;

            Logger.CompleteIfRoot();
            Logger.TrySetContext(_parent);

            Unloaded?.Invoke(this);
        }
    }
}
