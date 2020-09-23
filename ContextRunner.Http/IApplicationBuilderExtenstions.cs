using ContextRunner.Http.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace ContextRunner.Http
{
    public static class IApplicationBuilderExtenstions
    {
        public static void UseContextRunnerHttpMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ActionContextMiddleware>();
        }
    }
}
