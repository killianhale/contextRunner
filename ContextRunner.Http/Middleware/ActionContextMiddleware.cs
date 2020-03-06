using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ContextRunner.Http.Middleware
{
    public class ActionContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IContextRunner _runner;

        public ActionContextMiddleware(RequestDelegate next, IContextRunner runner)
        {
            _next = next;
            _runner = runner;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var routeData = httpContext?.Request?.RouteValues;
            var controller = routeData == null ? null : routeData["controller"];
            var action = routeData == null ? null : routeData["action"];

            if (controller == null || action == null)
            {
                await _next(httpContext);
                return;
            }

            await _runner.RunAction(async context =>
            {
                var ignoreList = new[] { "Connection", "Accept-Encoding", "Accept-Language", "Content-Length", "Sec-Fetch-Site", "Sec-Fetch-Mode" };

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
            }, $"{controller}_{action}");
        }
    }
}
