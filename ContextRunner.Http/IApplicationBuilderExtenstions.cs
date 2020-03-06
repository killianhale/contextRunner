using ContextRunner.Http.Middleware;
using Microsoft.AspNetCore.Builder;

namespace ContextRunner.Http
{
    public static class IApplicationBuilderExtenstions
    {
        public static void AddContextMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ActionContextMiddleware>();
        }
    }
}
