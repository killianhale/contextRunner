using System;
using Microsoft.Extensions.Logging;

namespace ContextRunner.Http
{
    public class ActionContextMiddlewareConfig
    {
        public ActionContextMiddlewareConfig()
        {
            PathPrefixWhitelist = "/api/";
            HttpErrorCodeMessageLogLevel = LogLevel.Warning;
            HttpSuccessCodeMessageLogLevel = LogLevel.Information;
        }

        public string PathPrefixWhitelist { get; set; }
        public bool PrintLogLineForHttpErrorCodes { get; set; }
        public LogLevel HttpErrorCodeMessageLogLevel { get; set; }
        public bool PrintLogLineForHttpSuccessCodes { get; set; }
        public LogLevel HttpSuccessCodeMessageLogLevel { get; set; }
    }
}
