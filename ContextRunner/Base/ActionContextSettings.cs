using System;
using Microsoft.Extensions.Logging;

namespace ContextRunner.Base
{
    public class ActionContextSettings
    {
        public ActionContextSettings()
        {
            WrapExceptionsWithContext = true;

            EnableContextStartMessage = false;
            SuppressChildContextEndMessages = false;
            ContextStartMessageLevel = LogLevel.Trace;

            EnableContextEndMessage = true;
            SuppressChildContextEndMessages = false;
            ContextEndMessageLevel = LogLevel.Trace;

            ContextErrorMessageLevel = LogLevel.Error;

            SuppressContextByNameList = new string[0];
            SuppressContextsByNameUnderLevel = LogLevel.Warning;
        }

        public bool WrapExceptionsWithContext { get; set; }

        public bool EnableContextStartMessage { get; set; }
        public bool EnableContextEndMessage { get; set; }

        public bool SuppressChildContextStartMessages { get; set; }
        public bool SuppressChildContextEndMessages { get; set; }

        public LogLevel ContextStartMessageLevel { get; set; }
        public LogLevel ContextEndMessageLevel { get; set; }

        public LogLevel ContextErrorMessageLevel { get; set; }

        public string[] SuppressContextByNameList { get; set; }
        public LogLevel SuppressContextsByNameUnderLevel { get; set; }
    }
}
