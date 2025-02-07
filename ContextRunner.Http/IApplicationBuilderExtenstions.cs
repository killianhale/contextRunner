using ContextRunner.Http.Middleware;
using Microsoft.AspNetCore.Builder;

namespace ContextRunner.Http
{
    public static class ApplicationBuilderExtenstions
    {
        public static void UseContextRunnerHttpMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ActionContextMiddleware>();
        }
    }
}
