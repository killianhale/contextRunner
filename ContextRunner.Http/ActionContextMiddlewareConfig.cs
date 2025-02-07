using Microsoft.Extensions.Logging;

namespace ContextRunner.Http
{
    public class ActionContextMiddlewareConfig
    {
        public string PathPrefixWhitelist { get; init; } = "/api/";
        public bool PrintLogLineForHttpErrorCodes { get; init; }
        public LogLevel HttpErrorCodeMessageLogLevel { get; init; } = LogLevel.Warning;
        public bool PrintLogLineForHttpSuccessCodes { get; init; }
        public LogLevel HttpSuccessCodeMessageLogLevel { get; init; } = LogLevel.Information;
    }
}
