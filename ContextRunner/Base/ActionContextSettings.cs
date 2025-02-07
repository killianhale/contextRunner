namespace ContextRunner.Base
{
    public class ActionContextSettings
    {
        public bool EnableContextStartMessage { get; init; }
        public bool EnableContextEndMessage { get; init; } = true;

        public bool SuppressChildContextStartMessages { get; init; }
        public bool SuppressChildContextEndMessages { get; init; }

        public bool AlwaysShowContextEndMessagesOnError { get; init; } = true;
        public bool AlwaysShowContextStartMessagesOnError { get; init; }

        public LogLevel ContextStartMessageLevel { get; init; } = LogLevel.Trace;
        public LogLevel ContextEndMessageLevel { get; init; } = LogLevel.Trace;

        public LogLevel ContextErrorMessageLevel { get; init; } = LogLevel.Error;

        public string[] SuppressContextByNameList { get; init; } = [];
        public LogLevel SuppressContextsByNameUnderLevel { get; init; } = LogLevel.Warning;

        public string[] PropertiesToAddToSummaryList { get; init; } = [];
    }
}
