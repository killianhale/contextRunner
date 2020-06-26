using System;
using ContextRunner.Base;
using NLog;

namespace ContextRunner.NLog
{
    public class NlogContextRunnerConfig
    {
        public NlogContextRunnerConfig()
        {
            ContextLogNamePrefix = "context_";
            EntryLogNamePrefix = "entry_";

            EnableContextStartMessage = false;
            SuppressChildContextEndMessages = false;
            ContextStartMessageLevel = LogLevel.Trace;

            EnableContextEndMessage = true;
            SuppressChildContextEndMessages = false;
            ContextEndMessageLevel = LogLevel.Trace;

            ContextErrorMessageLevel = LogLevel.Error;

            SuppressContextByNameList = new string[0];
            SuppressContextsByNameUnderLevel = LogLevel.Warn;
        }

        public string ContextLogNamePrefix { get; set; }
        public string EntryLogNamePrefix { get; set; }

        public bool EnableContextStartMessage { get; set; }
        public bool EnableContextEndMessage { get; set; }

        public bool SuppressChildContextStartMessages { get; set; }
        public bool SuppressChildContextEndMessages { get; set; }

        public LogLevel ContextStartMessageLevel { get; set; }
        public LogLevel ContextEndMessageLevel { get; set; }

        public LogLevel ContextErrorMessageLevel { get; set; }

        public string[] SuppressContextByNameList { get; set; }
        public LogLevel SuppressContextsByNameUnderLevel { get; set; }

        public bool AddSpacingToEntries { get; set; }
        public string[] SanitizedProperties { get; set; }
    }
}
