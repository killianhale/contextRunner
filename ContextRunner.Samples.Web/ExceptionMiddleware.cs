using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using ContextRunner.Logging;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ContextRunner.Samples.Web
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                if (httpContext.Response.HasStarted)
                {
                    throw;
                }

                var lookupException = ex;

                var statusCode = HttpStatusCode.InternalServerError;

                var message = lookupException.Message;
                var stackTrace = lookupException.StackTrace;

                if (lookupException is TimeoutException)
                {
                    statusCode = HttpStatusCode.GatewayTimeout;
                }
                //else if (lookupException is DataNotFoundException)
                //{
                //    statusCode = HttpStatusCode.NotFound;
                //}
                //else if (lookupException is DataConflictException)
                //{
                //    statusCode = HttpStatusCode.Conflict;
                //}
                else if (lookupException is ArgumentException)
                {
                    statusCode = HttpStatusCode.BadRequest;
                }
                else if (lookupException is AuthenticationException)
                {
                    statusCode = HttpStatusCode.Unauthorized;
                }

                httpContext.Response.Clear();
                httpContext.Response.StatusCode = (int)statusCode;
                httpContext.Response.ContentType = "application/json";

                var roContextParams = ex.Data["ContextParams"] as IReadOnlyDictionary<string, object>;
                var contextParams = roContextParams.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                contextParams.Remove("Exception");

                var entries = ex.Data["ContextEntries"] as ContextLogEntry[];

                var body = new
                {
                    Message = message,
                    StackTrance = stackTrace,
                    ExceptionData = new
                    {
                        ContextParams = contextParams,
                        ContextEntries = entries
                    }
                };

                var responseStr = JsonConvert.SerializeObject(body,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    });


                await httpContext.Response.WriteAsync(responseStr);
                await httpContext.Response.Body.FlushAsync();
            }
        }
    }
}
