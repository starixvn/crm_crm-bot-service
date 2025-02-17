using CRM.BotService.Core.Enum;
using CRM.BotService.Core.Models.Base;
using CRM.BotService.Core.Models.Notification;
using CRM.BotService.Core.Services;

namespace CRM.BotService.Endpoint.UnitTests.Telegram
{
    using AutoFixture;
    using Core.Constants;
    using Core.Services.Interfaces;
    using CRM.BotService.Endpoint.Controllers.Telegram;
    using Fanex.Caching;
    using global::Telegram.Bot.Types;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using NSubstitute;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ViewModel;
    using Xunit;

    public class MessageControllerTests
    {
        private ILogServiceExtend logService;
        private Func<long, ITelegramBotService> telegramBotService;
        private IBotCommonService botCommonService;
        private IConfiguration configuration;
        private ICacheService cacheService;
        private readonly Fixture fixture = new Fixture();

        [Fact]
        public async Task SendMultipleMessage_WhenCalled_ExpectReturnResult()
        {
            // Arrange
            var notifications = fixture.Build<NotificationMessage>().CreateMany(1);
            var controller = GetController();
            var notification = notifications.First();
            botCommonService.IsActiveChannel(notification.ChannelId).Returns(true);
            var expected = new SendResult
            {
                NotificationId = notification.NotificationId,
                ConversationGroupId = notification.ConversationGroupId,
            };

            // Act
            var actual = await controller.SendMultipleMessage(notifications);
            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as IEnumerable<SendResult>;
            var result = actualResult.First();
            // Assert
            logService.Received(5).LogInformation(Arg.Any<string>());
            Assert.Equal(result.NotificationId, expected.NotificationId);
            Assert.Equal(result.ConversationGroupId, expected.ConversationGroupId);
        }

        [Fact]
        public async Task SendMultipleMessage_WhenCalledInactiveChannel_ExpectCallExactFunction()
        {
            // Arrange
            var notifications = fixture.Build<NotificationMessage>().CreateMany(1);
            var controller = GetController();
            var expected = new SendResult
            {
                NotificationId = notifications.First().NotificationId,
                ConversationGroupId = notifications.First().ConversationGroupId,
                ErrorCode = 1,
                ErrorMessage = $"Channel {notifications.First().ChannelId} is current inactivated",
                IsIgnoreRetry = true
            };
            // Act
            var actual = await controller.SendMultipleMessage(notifications);
            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as IEnumerable<SendResult>;
            var result = actualResult.First();
            // Assert
            logService.Received(4).LogInformation(Arg.Any<string>());
            Assert.Equal(result.NotificationId, expected.NotificationId);
            Assert.Equal(result.ConversationGroupId, expected.ConversationGroupId);
            Assert.Equal(result.ErrorCode, expected.ErrorCode);
            Assert.Equal(result.ErrorMessage, expected.ErrorMessage);
            Assert.Equal(result.IsIgnoreRetry, expected.IsIgnoreRetry);
        }

        [Fact]
        public async Task SendMultipleFormattedTextAndImagesAsync_WhenCalledInactiveChannel_ExpectCallExactFunction()
        {
            // Arrange
            var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1);
            var controller = GetController();
            var expected = new SendNotificationResult
            {
                AnnouncementId = notifications.First().AnnouncementId,
                ConversationId = notifications.First().ConversationId,
                IsSuccessful = false,
                ErrMsg = $"Channel {notifications.First().ChannelId} is current inactivated",
                IgnoredRetry = true
            };
            // Act
            var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);
            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as IEnumerable<SendNotificationResult>;
            var result = actualResult.First();
            // Assert
            logService.Received(2).LogInformation(Arg.Any<string>());
            Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
            Assert.Equal(result.ConversationId, expected.ConversationId);
            Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
            Assert.Equal(result.ErrMsg, expected.ErrMsg);
            Assert.Equal(result.IgnoredRetry, expected.IgnoredRetry);
        }

        [Fact]
        public async Task SendMultipleFormattedTextAndImagesAsync_ActiveChannelAndSendSuccesfull_ExpectWriteLogAndReturnExactResult()
        {
            // Arrange
            var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1);
            var controller = GetController();
            var expected = new SendNotificationResult
            {
                AnnouncementId = notifications.First().AnnouncementId,
                ConversationId = notifications.First().ConversationId,
                IsSuccessful = true
            };
            var message = new Message
            {
                Chat = new Chat { Id = 1, Title = "chat title" },
                From = new User { Id = 1 }
            };
            botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
            telegramBotService(Arg.Any<long>()).SendTextMessageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(message);

            // Act
            var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);

            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as IEnumerable<SendNotificationResult>;
            var result = actualResult.First();
            // Assert
            logService.Received(16).LogInformation(Arg.Any<string>());
            Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
            Assert.Equal(result.ConversationId, expected.ConversationId);
            Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
        }

        //[Fact]
        //public async Task SendMultipleFormattedTextAndImagesAsync_ActiveChannelAndSendThrowException_ExpectWriteLogAndReturnExactResult()
        //{
        //    // Arrange
        //    var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1);
        //    var controller = GetController();
        //    var expected = new SendNotificationResult
        //    {
        //        AnnouncementId = notifications.First().AnnouncementId,
        //        ConversationId = notifications.First().ConversationId,
        //        IsSuccessful = false,
        //        ErrMsg = ErrorMessageTelegram.MessageIsTooLong,
        //        IgnoredRetry = true
        //    };
        //    var message = new Message
        //    {
        //        Chat = new Chat { Id = 1, Title = "chat title" },
        //        From = new User { Id = 1 }
        //    };
        //    botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
        //    telegramBotService(Arg.Any<long>()).When(x => x.SendTextMessageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()))
        //        .Do(x => { throw new Exception("Message is too long"); });

        //    // Act
        //    var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);

        //    var okObjectResult = actual as OkObjectResult;
        //    var actualResult = okObjectResult.Value as IEnumerable<SendNotificationResult>;
        //    var result = actualResult.First();
        //    // Assert
        //logService.Received(16).LogInformation(Arg.Any<string>());
        //    await logService.Received(1).ErrorAsync(Arg.Any<Exception>());
        //    Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
        //    Assert.Equal(result.ConversationId, expected.ConversationId);
        //    Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
        //    Assert.Equal(result.ErrMsg, expected.ErrMsg);
        //    Assert.Equal(result.IgnoredRetry, expected.IgnoredRetry);
        //}

        [Fact]
        public async Task SendMultipleFormattedTextAndImagesAsync_SendTextSucessfullAndHaveImages_ExpectedSendImage()
        {
            // Arrange
            var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1);
            notifications.First().Images = new List<string> { "test" };
            var controller = GetController();
            var expected = new SendNotificationResult
            {
                AnnouncementId = notifications.First().AnnouncementId,
                ConversationId = notifications.First().ConversationId,
                IsSuccessful = true
            };
            var message = new Message
            {
                Chat = new Chat { Id = 1, Title = "chat title" },
                From = new User { Id = 1 }
            };
            botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
            telegramBotService(Arg.Any<long>()).SendTextMessageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(message);
            telegramBotService(Arg.Any<long>()).SendImageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(message);

            // Act
            var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);

            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as IEnumerable<SendNotificationResult>;
            var result = actualResult.First();
            // Assert
            logService.Received(8).LogInformation(Arg.Any<string>());
            Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
            Assert.Equal(result.ConversationId, expected.ConversationId);
            Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
        }

        [Fact]
        public async Task SendMultipleFormattedTextAndImagesAsync_SendTextSucessfullAndSendImageThrowException_ExpectedWriteLogAndReturnExactResult()
        {
            // Arrange
            var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1);
            notifications.First().Images = new List<string> { "test" };
            var controller = GetController();
            var expected = new SendNotificationResult
            {
                AnnouncementId = notifications.First().AnnouncementId,
                ConversationId = notifications.First().ConversationId,
                IsSuccessful = false,
                ErrMsg = ErrorMessageTelegram.BlockBot,
                IgnoredRetry = true,
                IsBotBlocked = true,
                IsOnlyRetryImage = true,
                ErrImages = new List<string> { "test" }
            };
            var message = new Message
            {
                Chat = new Chat { Id = 1, Title = "chat title" },
                From = new User { Id = 1 }
            };
            botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
            telegramBotService(Arg.Any<long>()).SendTextMessageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(message);
            telegramBotService(Arg.Any<long>()).When(x => x.SendImageStreamAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<Stream>()))
                .Do(x => { throw new Exception("bot was blocked by the user"); });

            // Act
            var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);

            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as IEnumerable<SendNotificationResult>;
            var result = actualResult.First();
            // Assert
            logService.Received(8).LogInformation(Arg.Any<string>());
            await logService.Received(1).ErrorAsync(Arg.Any<Exception>());
            Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
            Assert.Equal(result.ConversationId, expected.ConversationId);
            Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
            Assert.Equal(result.ErrMsg, expected.ErrMsg);
            Assert.Equal(result.IsBotBlocked, expected.IsBotBlocked);
            Assert.Equal(result.IsOnlyRetryImage, expected.IsOnlyRetryImage);
            Assert.Equal(result.ErrImages, expected.ErrImages);
        }

        [Fact]
        public async Task SendMultipleImagesOnlyAsync_WhenCalledInactiveChannel_ExpectCallExactFunction()
        {
            // Arrange
            var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1);
            var controller = GetController();
            var expected = new SendNotificationResult
            {
                AnnouncementId = notifications.First().AnnouncementId,
                ConversationId = notifications.First().ConversationId,
                IsSuccessful = false,
                ErrMsg = $"Channel {notifications.First().ChannelId} is current inactivated",
                IgnoredRetry = true
            };
            // Act
            var actual = await controller.SendMultipleImagesOnlyAsync(notifications);
            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as IEnumerable<SendNotificationResult>;
            var result = actualResult.First();
            // Assert
            logService.Received(2).LogInformation(Arg.Any<string>());
            Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
            Assert.Equal(result.ConversationId, expected.ConversationId);
            Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
            Assert.Equal(result.ErrMsg, expected.ErrMsg);
            Assert.Equal(result.IgnoredRetry, expected.IgnoredRetry);
        }

        [Fact]
        public async Task SendMultipleImagesOnlyAsync_ActiveChannelAndSendSuccesfull_ExpectWriteLogAndReturnExactResult()
        {
            // Arrange
            var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1);
            notifications.First().RetryImages = new List<string> { "test" };
            var controller = GetController();
            var expected = new SendNotificationResult
            {
                AnnouncementId = notifications.First().AnnouncementId,
                ConversationId = notifications.First().ConversationId,
                IsSuccessful = true
            };
            var message = new Message
            {
                From = new User { Id = 1 },
                Photo = new PhotoSize[]
                {
                    new PhotoSize
                     {
                         FileId = "FileId"
                     }
                }
            };
            botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
            telegramBotService(Arg.Any<long>()).SendImageStreamAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<Stream>()).Returns(message);

            // Act
            var actual = await controller.SendMultipleImagesOnlyAsync(notifications);

            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as IEnumerable<SendNotificationResult>;
            var result = actualResult.First();
            // Assert
            logService.Received(6).LogInformation(Arg.Any<string>());
            Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
            Assert.Equal(result.ConversationId, expected.ConversationId);
            Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
        }

        [Fact]
        public async Task SendSingleMessage_WhenCalled_ExpectReturnResult()
        {
            // Arrange
            var notification = fixture.Create<NotificationMessage>();
            var controller = GetController();
            botCommonService.IsActiveChannel(notification.ChannelId).Returns(true);
            var expected = new SendResult
            {
                NotificationId = notification.NotificationId,
                ConversationGroupId = notification.ConversationGroupId,
            };

            // Act
            var actual = await controller.SendSingleMessage(notification);
            var okObjectResult = actual as OkObjectResult;
            var actualResult = okObjectResult.Value as SendResult;
            // Assert
            Assert.Equal(actualResult.NotificationId, expected.NotificationId);
            Assert.Equal(actualResult.ConversationGroupId, expected.ConversationGroupId);
        }

        [Fact]
        public async Task CheckPersonalProfileAsync_WhenCalled_ExpectCallExactFunction()
        {
            // Arrange
            var telegramBot = fixture.Create<TelegramBotConversationViewModel>();
            telegramBot.ChatId = 1;
            telegramBot.BotId = 1;
            var controller = GetController();
            // Act
            await controller.CheckPersonalProfileAsync(telegramBot);

            // Assert
            await telegramBotService(telegramBot.BotId).Received(1).GetChatAsync(telegramBot.BotId, telegramBot.ChatId);
        }

        [Fact]
        public async Task CheckPersonalProfileAsync_WhenHasError_ExpectWriteLogError()
        {
            // Arrange
            var telegramBot = fixture.Create<TelegramBotConversationViewModel>();
            telegramBot.ChatId = 1;
            telegramBot.BotId = 1;
            var controller = GetController();
            telegramBotService(telegramBot.BotId)
                .When(x => x.GetChatAsync(telegramBot.BotId, telegramBot.ChatId))
                .Do(x => { throw new Exception("get chat failed"); });
            // Act
            await controller.CheckPersonalProfileAsync(telegramBot);

            // Assert
            await logService.Received(1).ErrorAsync(Arg.Any<Exception>());
        }

        #region helper

        private MessageController GetController()
        {
            logService = Substitute.For<ILogServiceExtend>();
            telegramBotService = Substitute.For<Func<long, ITelegramBotService>>();
            botCommonService = Substitute.For<IBotCommonService>();
            configuration = Substitute.For<IConfiguration>();
            cacheService = Substitute.For<ICacheService>();
            return new MessageController(
                logService,
                telegramBotService,
                botCommonService,
                configuration,
                cacheService
            );
        }

        #endregion helper
    }
}