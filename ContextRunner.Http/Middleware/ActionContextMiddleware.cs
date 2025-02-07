using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace ContextRunner.Http.Middleware
{
    public class ActionContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IContextRunner _runner;
        private readonly ActionContextMiddlewareConfig _config;

        public ActionContextMiddleware(RequestDelegate next, IContextRunner? runner = null, IOptionsMonitor<ActionContextMiddlewareConfig>? configMonitor = null)
        {
            _next = next;
            _runner = runner ?? ActionContextRunner.Runner;
            _config = configMonitor?.CurrentValue ?? new ActionContextMiddlewareConfig();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var routeData = httpContext.Request.RouteValues;
            var controller = routeData["controller"];
            var action = routeData["action"];

            var name = controller != null && action != null
                ? $"{controller}_{action}"
                : null;

            if (name == null)
            {
                if(IsPathWhitelisted(httpContext.Request.Path))
                {
                    name = httpContext.Request.Path.ToString();
                    name = name.Substring(_config.PathPrefixWhitelist.Length);
                    name = name.Replace('/', '_');
                    name += $"_{httpContext.Request.Method}";
                }
                else
                {
                    await _next(httpContext);
                    return;
                }
            }

            await _runner.CreateAndAppendToActionExceptionsAsync(async context =>
            {
                var requestInfo = GetFilteredHeaders(httpContext.Request.Headers);
                requestInfo["Path"] = httpContext.Request.Path.Value;
                requestInfo["Method"] = httpContext.Request.Method;

                if(httpContext.Request.ContentType?.IndexOf("json") >= 0)
                {
                    var requestBody = await GetRequestBody(httpContext.Request);

                    requestInfo["Body"] = GetFilteredBody(requestBody);
                }

                context.State.SetParam("httpRequest", requestInfo);

                var responseBody = await RunNext(httpContext);

                var statusCode = httpContext.Response.StatusCode;
                var responseInfo = GetFilteredHeaders(httpContext.Response.Headers);
                responseInfo["StatusCode"] = statusCode;
                responseInfo["Body"] = GetFilteredBody(responseBody);

                context.State.SetParam("httpResponse", responseInfo);

                if (statusCode >= 400 && _config.PrintLogLineForHttpErrorCodes)
                {
                    context.Logger.Log(_config.HttpErrorCodeMessageLogLevel, $"The HTTP request resulted in an {statusCode} response code.");
                }
                else if (statusCode < 400 && _config.PrintLogLineForHttpSuccessCodes)
                {
                    context.Logger.Log(_config.HttpSuccessCodeMessageLogLevel, $"The HTTP request resulted in an {statusCode} response code.");
                }

            }, name);
        }

        private bool IsPathWhitelisted(PathString path)
        {
            var whitelistPath = !string.IsNullOrEmpty(_config.PathPrefixWhitelist)
                ? _config.PathPrefixWhitelist
                : null;
            var pathString = path.ToString();

            var isWhitelisted = whitelistPath != null && pathString.StartsWith(whitelistPath);

            return isWhitelisted;
        }

        private async Task<string> GetRequestBody(HttpRequest request)
        {
            string result;

            request.EnableBuffering();

            using (var reader
                      = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                result = await reader.ReadToEndAsync();
            }

            request.Body.Position = 0;

            return result;
        }

        private Dictionary<string, object?> GetFilteredHeaders(IHeaderDictionary headers)
        {
            var ignoreList = new[] {
                    "Cookie",
                    "Connection",
                    "Accept-Encoding",
                    "Accept-Language",
                    "Transfer-Encoding",
                    "Content-Length",
                    "Sec-Fetch-Site",
                    "Sec-Fetch-User",
                    "Sec-Fetch-Dest",
                    "Sec-Fetch-Mode",
                    "Upgrade-Insecure-Requests"
                };

            Dictionary<string, object?> result = headers
                .Where(header => !ignoreList.Contains(header.Key))
                .ToDictionary(
                    header => header.Key, object? (header) => header.Value.ToString()
                    );

            return result;
        }

        private object? GetFilteredBody(string responseBody)
        {
            if(string.IsNullOrEmpty(responseBody))
            {
                return null;
            }

            var token = JToken.Parse(responseBody);

            return token;
        }

        private async Task<string> RunNext(HttpContext httpContext)
        {
            var originalBody = httpContext.Response.Body;

            string? responseBody;

            try
            {
                using var memStream = new MemoryStream();
                httpContext.Response.Body = memStream;

                await _next(httpContext);

                memStream.Position = 0;
                responseBody = await new StreamReader(memStream).ReadToEndAsync();

                memStream.Position = 0;
                await memStream.CopyToAsync(originalBody);
            }
            finally
            {
                httpContext.Response.Body = originalBody;
            }

            return responseBody;
        }
    }
}
