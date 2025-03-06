using Fanex.Logging;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Endpoint.Tests
{
    public class ErrorHandlerTests
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly ErrorHandler errorHandler;

        public ErrorHandlerTests()
        {
            next = Substitute.For<RequestDelegate>();
            logger = Substitute.For<ILogger>();
            errorHandler = new ErrorHandler(next, logger);
        }

        [Fact]
        public async Task InvokeAsync_NoException_CallsNextDelegate()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            await errorHandler.InvokeAsync(context);

            // Assert
            await next.Received(1).Invoke(context);
        }

        [Fact]
        public async Task InvokeAsync_ExceptionThrown_HandlesException()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var exception = new Exception("Test exception");
            next.When(x => x.Invoke(context)).Do(_ => throw exception);

            // Act
            await errorHandler.InvokeAsync(context);

            // Assert
            await logger.Received(1).ErrorAsync(Arg.Is<string>(s => s.Contains("Test exception")));
        }

        [Fact]
        public async Task HandleException_AjaxRequest_SetsStatusCode500()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Method = "POST"
                }
            };
            context.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
            var exception = new Exception("Test exception");
            next.When(x => x.Invoke(context)).Do(_ => throw exception);

            // Act
            await errorHandler.InvokeAsync(context);

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        }

        [Fact]
        public async Task HandleException_NonAjaxRequest_DoesNotSetStatusCode()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Method = "GET"
                }
            };

            // Act
            await errorHandler.InvokeAsync(context);

            // Assert
            Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        }
    }
}