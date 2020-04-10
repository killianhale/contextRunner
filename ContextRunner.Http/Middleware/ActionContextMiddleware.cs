using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ContextRunner.Http.Middleware
{
    public class ActionContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IContextRunner _runner;
        private readonly IOptionsMonitor<ActionContextMiddlewareConfig> _configMonitor;

        public ActionContextMiddleware(RequestDelegate next, IContextRunner runner, IOptionsMonitor<ActionContextMiddlewareConfig> configMonitor)
        {
            _next = next;
            _runner = runner;
            _configMonitor = configMonitor;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var routeData = httpContext?.Request?.RouteValues;
            var controller = routeData == null ? null : routeData["controller"];
            var action = routeData == null ? null : routeData["action"];

            var name = controller != null && action != null
                ? $"{controller}_{action}"
                : null;

            if (name == null)
            {
                if(IsPathWhitelisted(httpContext.Request.Path))
                {
                    name = httpContext.Request.Path.ToString();
                    name = name.Substring(_configMonitor.CurrentValue.PathPrefixWhitelist.Length);
                    name = name.Replace('/', '_');
                    name += $"_{httpContext.Request.Method}";
                }
                else
                {
                    await _next(httpContext);
                    return;
                }
            }

            await _runner.RunAction(async context =>
            {
                var ignoreList = new[] { "Cookie", "Connection", "Accept-Encoding", "Accept-Language", "Content-Length", "Sec-Fetch-Site", "Sec-Fetch-Mode" };

                var requestInfo = httpContext.Request.Headers
                    .Where(header => !ignoreList.Contains(header.Key))
                    .ToDictionary(
                        header => header.Key,
                        header => header.Value.ToString()
                        );

                requestInfo["Path"] = httpContext.Request.Path.Value;
                requestInfo["Method"] = httpContext.Request.Method;

                context.State.SetParam("request", requestInfo);

                await _next(httpContext);
            }, name);
        }

        private bool IsPathWhitelisted(PathString path)
        {
            var config = _configMonitor.CurrentValue;
            var whitelistPath = !string.IsNullOrEmpty(config?.PathPrefixWhitelist)
                ? config.PathPrefixWhitelist
                : null;
            var pathString = path.ToString();

            var isWhitelisted = whitelistPath != null && pathString.StartsWith(whitelistPath);

            return isWhitelisted;
        }
    }
}
