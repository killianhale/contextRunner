using NLog;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ContextRunner.NLog.Internal
{
    internal static class NlogLogLevelUtil
    {
        public static LogLevel ConvertFromMsLogLevel(MsLogLevel level)
        {
            return level switch
            {
                MsLogLevel.Trace => LogLevel.Trace,
                MsLogLevel.Debug => LogLevel.Debug,
                MsLogLevel.Information => LogLevel.Info,
                MsLogLevel.Warning => LogLevel.Warn,
                MsLogLevel.Error => LogLevel.Error,
                MsLogLevel.Critical => LogLevel.Fatal,
                _ => LogLevel.Off,
            };
        }

        public static MsLogLevel ConvertToMsLogLevel(LogLevel level)
        {
            var result = MsLogLevel.None;

            if (level == LogLevel.Trace)
            {
                result = MsLogLevel.Trace;
            }
            else if (level == LogLevel.Debug)
            {
                result = MsLogLevel.Debug;
            }
            else if (level == LogLevel.Info)
            {
                result = MsLogLevel.Information;
            }
            else if (level == LogLevel.Warn)
            {
                result = MsLogLevel.Warning;
            }
            else if (level == LogLevel.Error)
            {
                result = MsLogLevel.Error;
            }
            else if (level == LogLevel.Fatal)
            {
                result = MsLogLevel.Critical;
            }

            return result;
        }
    }
}