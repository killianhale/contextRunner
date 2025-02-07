using System;
using System.IO;
using System.Reflection;
using ContextRunner;
using ContextRunner.Http;
using ContextRunner.NLog;
using ContextRunner.Samples.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NLog.Web;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerUI;

var apiAssembly = Assembly.GetExecutingAssembly();

var builder = WebApplication.CreateBuilder();

builder.Logging.ClearProviders();
builder.Host.UseNLog(
    new NLogAspNetCoreOptions
    {
        RemoveLoggerFactoryFilter = false
    }
);

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerExamplesFromAssemblies(apiAssembly);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ContextRunner.Http Web Sample",
        Version = "v1",
        Description = "An sample API utilizing ContextRunner.Http."
    });
    
    var xmlFile = $"{apiAssembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                
    options.IncludeXmlComments(xmlPath, true);
});

builder.Services.Configure<NlogContextRunnerConfig>(builder.Configuration.GetSection("NlogContextRunner"));
builder.Services.Configure<ActionContextMiddlewareConfig>(builder.Configuration.GetSection("ContextRunnerHttp"));

// builder.Services.AddSingleton<IContextRunner, ActionContextRunner>();
builder.Services.AddSingleton<IContextRunner, NlogContextRunner>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRouting();
app.MapControllers();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocExpansion(DocExpansion.List);
    options.EnableDeepLinking();
                
    options.RoutePrefix = string.Empty;
    options.SwaggerEndpoint($"/swagger/v1/swagger.json", "Sample");
});

app.UseContextRunnerHttpMiddleware();
app.UseMiddleware<ExceptionMiddleware>();

await app.RunAsync();

// namespace ContextRunner.Samples.Web
// {
//     public class Program
//     {
//         public static void Main(string[] args)
//         {
//             CreateHostBuilder(args).Build().Run();
//         }
//
//         public static IHostBuilder CreateHostBuilder(string[] args) =>
//             Host.CreateDefaultBuilder(args)
//                 .ConfigureWebHostDefaults(webBuilder =>
//                 {
//                     webBuilder.ConfigureLogging(options =>
//                     {
//                         options.AddConsole();
//
//                         options.SetMinimumLevel(LogLevel.Trace);
//                     });
//                     //webBuilder.UseNLog();
//                     webBuilder.UseStartup<Startup>();
//                 });
//     }
// }
