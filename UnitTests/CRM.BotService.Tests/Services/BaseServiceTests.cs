using CRM.BotService.Services;
using Fanex.Data.Repository;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Tests.Services
{
    public class BaseServiceTests
    {
        private readonly IDynamicRepository dynamicRepository;
        private readonly BaseService baseService;

        public BaseServiceTests()
        {
            dynamicRepository = Substitute.For<IDynamicRepository>();
            baseService = new BaseService(dynamicRepository);
        }

        [Fact]
        public async Task ExecuteNonQueryAsync_CommandIsNull_ReturnsDefaultCommandResult()
        {
            // Act
            var result = await baseService.ExecuteNonQueryAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Code);
            Assert.Equal("Invalid data.", result.Message);
        }

        [Fact]
        public async Task ExecuteNonQueryAsync_CommandIsInvalid_ReturnsDefaultCommandResult()
        {
            // Arrange
            var command = Substitute.For<INonQueryCommand>();
            command.IsValid().Returns(false);

            // Act
            var result = await baseService.ExecuteNonQueryAsync(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Code);
            Assert.Equal("Invalid data.", result.Message);
        }

        [Fact]
        public async Task ExecuteNonQueryAsync_CommandIsValid_ExecutesCommandAndReturnsCommandResult()
        {
            // Arrange
            var command = new FakeCommand
            {
                ErrCode = 1,
                ErrMsg = "Error message"
            };

            // Act
            var result = await baseService.ExecuteNonQueryAsync(command);

            // Assert
            await dynamicRepository.Received(1).ExecuteAsync(command);
            Assert.NotNull(result);
            Assert.NotEmpty(command.GetSettingKey());
            Assert.Equal(command.ErrCode, result.Code);
            Assert.Equal(command.ErrMsg, result.Message);
        }

        [Fact]
        public async Task ExecuteNonQueryAsync_CommandIsValid_PropertiesDoNotContainErrCode_ReturnsDefaultCommandResult()
        {
            // Arrange
            var command = Substitute.For<INonQueryCommand>();
            command.IsValid().Returns(true);

            // Act
            var result = await baseService.ExecuteNonQueryAsync(command);

            // Assert
            await dynamicRepository.Received(1).ExecuteAsync(command);
            Assert.NotNull(result);
            Assert.Equal(1, result.Code);
            Assert.Equal("Invalid data.", result.Message);
        }

        private class FakeCommand : INonQueryCommand
        {
            public int ErrCode { get; init; }
            public string ErrMsg { get; init; }

            public string GetSettingKey() => "FakeCommand";

            public bool IsValid() => true;
        }
    }
}