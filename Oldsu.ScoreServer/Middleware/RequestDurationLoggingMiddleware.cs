using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Oldsu
{
    // used to just log
    [Obsolete("Do not use this in any way shape or form. " +
              "This class is only used for logging purposes.")]
    public class RequestDurationLogging { }
}

namespace Oldsu.ScoreServer.Middleware
{
    public class RequestDurationLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestDurationLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = new Stopwatch();
            sw.Start();

            await _next(context);

            sw.Stop();
            await Global.LoggingManager.LogInfo<RequestDurationLogging>(
                $"Request to {context.Request.GetDisplayUrl()} took {sw.ElapsedMilliseconds}ms.");
        }
    }

    public static class RequestDurationLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestDurationLogging(
            this IApplicationBuilder builder) =>
            builder.UseMiddleware<RequestDurationLoggingMiddleware>();
    }
}