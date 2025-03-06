using CRM.BotService.Endpoint.Controllers.Telegram;
using CRM.BotService.Models;
using CRM.BotService.Services;
using CRM.BotService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace CRM.BotService.Endpoint.Tests.Telegram;

public class UpdateControllerTests
{
    private readonly ILogServiceExtend logService;
    private readonly Func<long, ITelegramBotService> telegramBotServiceFactory;
    private readonly UpdateController updateController;

    public UpdateControllerTests()
    {
        logService = Substitute.For<ILogServiceExtend>();
        telegramBotServiceFactory = Substitute.For<Func<long, ITelegramBotService>>();
        var baseService = Substitute.For<IBaseService>();
        List<TelegramBotConfigurations> botConfigurations = [new() { BotId = 1, Token = "token", Domain = "domain" }];
        updateController = new UpdateController(logService, telegramBotServiceFactory, baseService, botConfigurations);
    }

    [Fact]
    public async Task Post_UpdateParamIsNull_DoNothing()
    {
        // Arrange
        var update = new Update { Message = new Message { Chat = new() } };

        // Act
        var result = await updateController.Post(1, update);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        await telegramBotServiceFactory(Arg.Any<long>()).DidNotReceive().GetBotInfoAsync(1);
    }

    [Fact]
    public async Task Post_UpdateTypeNotEqualMessage_DoNothing()
    {
        // Arrange
        var update = new Update();

        // Act
        var result = await updateController.Post(1, update);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        await telegramBotServiceFactory(Arg.Any<long>()).DidNotReceive().GetBotInfoAsync(1);
    }

    [Fact]
    public async Task Post_BotIsNotExist_ReturnExpectedOkObjectResultValue()
    {
        // Arrange
        var update = new Update { Message = new Message { Chat = new() } };

        // Act
        var result = await updateController.Post(1, update);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal("Telegram Bot 1 is invalid.", okResult.Value);

        await logService.Received().ErrorMessageAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Post_ChatCommand_CallCorrectlyMethod()
    {
        // Arrange
        var bot = new User { Id = 1, Username = "bot" };
        var messageEntities = new MessageEntity[] { new() { Type = MessageEntityType.BotCommand, Offset = 0, Length = 6 } };
        var update = new Update
        {
            Message = new()
            {
                Text = "/start",
                Entities = messageEntities,
                Chat = new() { Type = ChatType.Private }
            }
        };
        telegramBotServiceFactory(Arg.Any<long>()).GetBotInfoAsync(1).Returns(bot);

        // Act
        var result = await updateController.Post(1, update);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        await telegramBotServiceFactory(Arg.Any<long>()).Received().SendTextMessageAsync(bot.Id, update.Message.Chat.Id, Arg.Any<string>());
    }

    [Fact]
    public async Task Post_GroupCreated_CallCorrectlyMethod()
    {
        // Arrange
        var update = new Update
        {
            Message = new()
            {
                GroupChatCreated = true,
                Chat = new() { Id = 6012438440 }
            }
        };
        var bot = new User { Id = 1, Username = "bot" };
        telegramBotServiceFactory(Arg.Any<long>()).GetBotInfoAsync(1).Returns(bot);

        // Act
        var result = await updateController.Post(1, update);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        await telegramBotServiceFactory(Arg.Any<long>()).Received().SendTextMessageAsync(bot.Id, update.Message.Chat.Id, Arg.Any<string>());
    }

    [Fact]
    public async Task Post_ChatMembersAdded_CallCorrectlyMethod()
    {
        // Arrange
        var bot = new User { Id = 1, Username = "bot" };
        var update = new Update
        {
            Message = new()
            {
                NewChatMembers = [new() { Username = "teddy.le", IsBot = false },
                    new() { Username = "bot1", IsBot = true },
                    new() { Username = "bot2", IsBot = true }]
            }
        };
        telegramBotServiceFactory(Arg.Any<long>()).GetBotInfoAsync(1).Returns(bot);

        // Act
        var result = await updateController.Post(1, update);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }
}