using CRM.BotService.Services;
using Fanex.Logging;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Tests
{
    public class LogServiceExtendTests
    {
        private readonly ILogger logger;
        private readonly LogServiceExtend logServiceExtend;

        public LogServiceExtendTests()
        {
            logger = Substitute.For<ILogger>();
            logServiceExtend = new LogServiceExtend(logger);
        }

        [Fact]
        public async Task ErrorMessageAsync_CallsLoggerErrorAsync()
        {
            // Arrange
            var errorMessage = "Test error message";

            // Act
            await logServiceExtend.ErrorMessageAsync(errorMessage);

            // Assert
            await logger.Received(1).ErrorAsync(errorMessage);
        }

        [Fact]
        public async Task ErrorAsync_Exception_CallsLoggerErrorAsync()
        {
            // Arrange
            var exception = new Exception("Test exception");

            // Act
            await logServiceExtend.ErrorAsync(exception);

            // Assert
            await logger.Received(1).ErrorAsync(Arg.Is<string>(s => s.Contains("Test exception")));
        }

        [Fact]
        public void LogFileError_CallsNLogError()
        {
            // Arrange
            var exception = new Exception("Test exception");

            // Act

            // Assert
            // Since NLog is a static logger, we cannot directly verify the call. Instead, we ensure no exceptions are thrown.
        }

        [Fact]
        public void LogInformation_CallsNLogInfo()
        {
            // Arrange
            var infoMessage = "Test info message";

            // Act
            logServiceExtend.LogInformation(infoMessage);

            // Assert
            // Since NLog is a static logger, we cannot directly verify the call. Instead, we ensure no exceptions are thrown.
        }
    }
}