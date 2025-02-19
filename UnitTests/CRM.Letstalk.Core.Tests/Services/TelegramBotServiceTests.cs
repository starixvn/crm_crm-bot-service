namespace CRM.BotService.Core.Tests.Services
{
    using AutoFixture;
    using Constants;
    using CRM.BotService.Core.Criteria;
    using CRM.BotService.Core.Services;
    using CRM.BotService.Core.Services.Interfaces;
    using Models;
    using Models.Base;
    using NSubstitute;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Telegram.Bot.Types.Enums;
    using Xunit;

    public class TelegramBotServiceTests
    {
        private IList<TelegramBotConfigurations> botConfigurations;
        private ILogServiceExtend logService;
        private IBotCommonService botCommonService;
        private IHttpClientFactory httpClientFactory;
        private readonly IFixture fixture = new Fixture();

        [Fact]
        public async Task SendTextMessageAsync_WhenCalled_ReturnsExactMessage()
        {
            // Arrange
            var botId = 12345678;
            var chatId = 987654321;
            var parseMode = ParseMode.Html;
            var service = GetService();
            // Act
            var result = await service.SendTextMessageAsync(botId, chatId, "messsage content", parseMode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("message reponse!", result.Text);
        }

        [Fact]
        public async Task SendImageAsync_WhenCalled_ReturnsExactMessage()
        {
            // Arrange
            var botId = 12345678;
            var chatId = 987654321;
            var service = GetService();
            // Act
            var result = await service.SendImageAsync(botId, chatId, "urlImage");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("message reponse!", result.Text);
        }

        [Fact]
        public async Task SendNotifyAysnc_WhenCalled_ShouldCallExactFunction()
        {
            // Arrange
            var result = new SendResult();
            var notificationMessage = new NotificationMessage
            {
                Content = "Content",
                ConversationId = "1234",
                BotId = 12345678
            };
            var service = GetService();
            // Act
            await service.SendNotifyAsync(notificationMessage, result);

            // Assert
            logService.Received(2).LogInformation(Arg.Any<string>());
            await botCommonService.Received().WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
            result.ErrorCode = 0;
        }

        [Fact]
        public async Task SendNotifyAysnc_NullObject_ShouldExpectedWriteLog()
        {
            // Arrange
            var result = new SendResult();
            var notificationMessage = new NotificationMessage
            {
                ConversationId = "1234",
                BotId = 12345678,
            };
            var service = GetService();
            // Act
            await service.SendNotifyAsync(notificationMessage, result);

            // Assert
            logService.Received(3).LogInformation(Arg.Any<string>());
            await botCommonService.Received(1).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
            result.ErrorCode = 1;
        }

        [Fact]
        public async Task WriteLogCommandAsync_WhenCalled_ShouldCallExactFunction()
        {
            // Arrange
            var result = fixture.Create<SendResult>();
            var notificationMessage = fixture.Create<NotificationMessage>();
            var service = GetService();
            // Act
            await service.WriteLogCommandAsync(notificationMessage, result);

            // Assert
            await botCommonService.Received(1).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
        }

        #region Helpers

        private TelegramBotService GetService()
        {
            httpClientFactory = Substitute.For<IHttpClientFactory>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            string jsonResponse = @"{
            ""ok"": true,
            ""result"": {
                    ""message_id"": 12345,
                    ""from"": {
                    ""id"": 67890,
                    ""is_bot"": false,
                    ""first_name"": ""John"",
                    ""last_name"": ""Doe"",
                    ""username"": ""johndoe"",
                    ""language_code"": ""en""
                    },
                    ""chat"": {
                    ""id"": 123456789,
                    ""first_name"": ""John"",
                    ""last_name"": ""Doe"",
                    ""username"": ""johndoe"",
                    ""type"": ""private""
                    },
                    ""date"": 1652030400,
                    ""text"": ""message reponse!""
            }
            }";

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(response));
            botConfigurations = new List<TelegramBotConfigurations>()
            {
                new TelegramBotConfigurations
                {
                    BotId = 12345678,
                    Token = "Token",
                    Domain="Domain/",
                }
            };
            var client = new HttpClient(mockHttpMessageHandler);
            httpClientFactory.CreateClient(NamedHttpClients.ProxiedClient).Returns(client);

            var botConfig = Substitute.For<List<TelegramBotConfigurations>>();
            botConfig.AddRange(botConfigurations);
            botCommonService = Substitute.For<IBotCommonService>();
            logService = Substitute.For<ILogServiceExtend>();
            return Substitute.For<TelegramBotService>(httpClientFactory, botConfig, logService, botCommonService);
        }

        #endregion Helpers
    }
}