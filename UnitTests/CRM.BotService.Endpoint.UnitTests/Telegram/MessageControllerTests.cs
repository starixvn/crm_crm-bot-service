using AutoFixture;
using CRM.BotService.Constants;
using CRM.BotService.Endpoint.Controllers.Telegram;
using CRM.BotService.Endpoint.ViewModel;
using CRM.BotService.Enum;
using CRM.BotService.Models.Base;
using CRM.BotService.Models.Notification;
using CRM.BotService.Services;
using CRM.BotService.Services.Interfaces;
using Fanex.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Xunit;

namespace CRM.BotService.Endpoint.Tests.Telegram;

public class MessageControllerTests
{
    private ILogServiceExtend logService;
    private Func<long, ITelegramBotService> telegramBotService;
    private IBotCommonService botCommonService;
    private IConfiguration configuration;
    private ICacheService cacheService;
    private readonly Fixture fixture = new();

    [Fact]
    public async Task SendMultipleMessage_WhenCalled_ExpectReturnResult()
    {
        // Arrange
        var notifications = fixture.Build<NotificationMessage>().CreateMany(1).ToList();
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
        var actualResult = okObjectResult?.Value as IEnumerable<SendResult>;
        var result = actualResult?.First();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.NotificationId, expected.NotificationId);
        Assert.Equal(result.ConversationGroupId, expected.ConversationGroupId);
    }

    [Fact]
    public async Task SendMultipleMessage_WhenCalledInactiveChannel_ExpectCallExactFunction()
    {
        // Arrange
        var notifications = fixture.Build<NotificationMessage>().CreateMany(1).ToList();
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
        var actualResult = okObjectResult?.Value as IEnumerable<SendResult>;
        var result = actualResult?.First();
        // Assert
        Assert.NotNull(result);
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
        var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1).ToList();
        var controller = GetController();
        var expected = new SendNotificationResult
        {
            AnnouncementId = notifications.First().AnnouncementId,
            ConversationId = notifications.First().ConversationId,
            IsSuccessful = false,
            ErrMsg = ErrorMessageTelegram.DisableChannelErrorMessage,
            IgnoredRetry = true
        };
        // Act
        var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);
        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as IEnumerable<SendNotificationResult>;
        var result = actualResult?.First();
        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
        Assert.Equal(result.ConversationId, expected.ConversationId);
        Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
        Assert.Equal(result.ErrMsg, expected.ErrMsg);
        Assert.Equal(result.IgnoredRetry, expected.IgnoredRetry);
    }

    [Fact]
    public async Task SendMultipleFormattedTextAndImagesAsync_ActiveChannelAndSendSuccessful_ExpectWriteLogAndReturnExactResult()
    {
        // Arrange
        var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1).ToList();
        var controller = GetController();
        var expected = new SendNotificationResult
        {
            AnnouncementId = notifications.First().AnnouncementId,
            ConversationId = notifications.First().ConversationId,
            IsSuccessful = true
        };
        var message = new Message
        {
            Chat = new() { Id = 1, Title = "chat title" },
            From = new() { Id = 1 }
        };
        botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
        telegramBotService(Arg.Any<long>()).SendTextMessageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(message);

        // Act
        var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);

        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as IEnumerable<SendNotificationResult>;
        var result = actualResult?.First();
        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
        Assert.Equal(result.ConversationId, expected.ConversationId);
        Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
    }

    [Fact]
    public async Task SendMultipleFormattedTextAndImagesAsync_SendTextSuccessfulAndHaveImages_ExpectedSendImage()
    {
        // Arrange
        var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1).ToList();
        notifications.First().Images = ["test"];
        var controller = GetController();
        var expected = new SendNotificationResult
        {
            AnnouncementId = notifications.First().AnnouncementId,
            ConversationId = notifications.First().ConversationId,
            IsSuccessful = true
        };
        var message = new Message
        {
            Chat = new() { Id = 1, Title = "chat title" },
            From = new() { Id = 1 }
        };
        botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
        telegramBotService(Arg.Any<long>()).SendTextMessageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(message);
        telegramBotService(Arg.Any<long>()).SendImageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(message);

        // Act
        var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);

        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as IEnumerable<SendNotificationResult>;
        var result = actualResult?.First();
        // Assert
        logService.Received().LogInformation(Arg.Any<string>());
        Assert.NotNull(result);
        Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
        Assert.Equal(result.ConversationId, expected.ConversationId);
        Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
    }

    [Fact]
    public async Task SendMultipleFormattedTextAndImagesAsync_SendTextSuccessfulAndSendImageThrowException_ExpectedWriteLogAndReturnExactResult()
    {
        // Arrange
        var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1).ToList();
        notifications.First().Images = ["test"];
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
            ErrImages = ["test"]
        };
        var message = new Message
        {
            Chat = new() { Id = 1, Title = "chat title" },
            From = new() { Id = 1 }
        };
        botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
        telegramBotService(Arg.Any<long>()).SendTextMessageAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(message);
        telegramBotService(Arg.Any<long>()).When(x => x.SendImageStreamAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<Stream>()))
            .Do(_ => throw new("bot was blocked by the user"));

        // Act
        var actual = await controller.SendMultipleFormattedTextAndImagesAsync(notifications);

        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as IEnumerable<SendNotificationResult>;
        var result = actualResult?.First();
        // Assert
        await logService.Received(1).ErrorAsync(Arg.Any<Exception>());
        Assert.NotNull(result);
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
        var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1).ToList();
        var controller = GetController();
        var expected = new SendNotificationResult
        {
            AnnouncementId = notifications.First().AnnouncementId,
            ConversationId = notifications.First().ConversationId,
            IsSuccessful = false,
            ErrMsg = ErrorMessageTelegram.DisableChannelErrorMessage,
            IgnoredRetry = true
        };

        // Act
        var actual = await controller.SendMultipleImagesOnlyAsync(notifications);
        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as IEnumerable<SendNotificationResult>;
        var result = actualResult?.First();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.AnnouncementId, expected.AnnouncementId);
        Assert.Equal(result.ConversationId, expected.ConversationId);
        Assert.Equal(result.IsSuccessful, expected.IsSuccessful);
        Assert.Equal(result.ErrMsg, expected.ErrMsg);
        Assert.Equal(result.IgnoredRetry, expected.IgnoredRetry);
    }

    [Fact]
    public async Task SendMultipleImagesOnlyAsync_ActiveChannelAndSendSuccessful_ExpectWriteLogAndReturnExactResult()
    {
        // Arrange
        var notifications = fixture.Build<AnnouncementNotification>().CreateMany(1).ToList();
        notifications.First().RetryImages = ["test"];
        var controller = GetController();
        var expected = new SendNotificationResult
        {
            AnnouncementId = notifications.First().AnnouncementId,
            ConversationId = notifications.First().ConversationId,
            IsSuccessful = true
        };
        var message = new Message
        {
            From = new() { Id = 1 },
            Photo =
            [
                new()
                {
                    FileId = "FileId"
                }
            ]
        };
        botCommonService.IsActiveChannel(BotChannel.Telegram).Returns(true);
        telegramBotService(Arg.Any<long>()).SendImageStreamAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<Stream>()).Returns(message);

        // Act
        var actual = await controller.SendMultipleImagesOnlyAsync(notifications);

        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as IEnumerable<SendNotificationResult>;
        var result = actualResult?.First();
        // Assert
        Assert.NotNull(result);
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

        var actualResult = okObjectResult?.Value as SendResult;
        // Assert
        Assert.NotNull(actualResult);
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
            .Do(_ => throw new("get chat failed"));
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
        return new(
            logService,
            telegramBotService,
            botCommonService,
            configuration,
            cacheService
        );
    }

    #endregion helper
}