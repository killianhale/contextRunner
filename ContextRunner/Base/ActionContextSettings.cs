using System;
using Microsoft.Extensions.Logging;

namespace ContextRunner.Base
{
    public class ActionContextSettings
    {
        public ActionContextSettings()
        {
            EnableContextStartMessage = false;
            SuppressChildContextEndMessages = false;
            ContextStartMessageLevel = LogLevel.Trace;

            EnableContextEndMessage = true;
            SuppressChildContextEndMessages = false;
            ContextEndMessageLevel = LogLevel.Trace;

            ContextErrorMessageLevel = LogLevel.Error;
        }

        public bool EnableContextStartMessage { get; set; }
        public bool EnableContextEndMessage { get; set; }

        public bool SuppressChildContextStartMessages { get; set; }
        public bool SuppressChildContextEndMessages { get; set; }

        public LogLevel ContextStartMessageLevel { get; set; }
        public LogLevel ContextEndMessageLevel { get; set; }

        public LogLevel ContextErrorMessageLevel { get; set; }
    }
}
