namespace CRM.BotService.Endpoint
{
    using Fanex.Logging;
    using Fanex.Logging.Extensions;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Threading.Tasks;

    public class ErrorHandler
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;

        public ErrorHandler(RequestDelegate next, ILogger logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleException(context, ex);
            }
        }

        private async Task HandleException(HttpContext context, Exception exception)
        {
            var logMessage = new LogMessage(exception.Message)
                .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
                .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
                .ToString();
            await logger.ErrorAsync(logMessage);

            var method = context.Request.Method.ToLowerInvariant();
            var requestedWith = context.Request.Headers["X-Requested-With"];

            if ((method == "post" || method == "put" || method == "delete") && requestedWith == "XMLHttpRequest")
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }
    }
}