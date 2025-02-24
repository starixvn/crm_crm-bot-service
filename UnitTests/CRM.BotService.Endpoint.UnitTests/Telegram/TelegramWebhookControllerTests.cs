using AutoFixture;
using CRM.BotService.Endpoint.Controllers.Telegram;
using CRM.BotService.Models;
using CRM.BotService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Xunit;

namespace CRM.BotService.Endpoint.Tests.Telegram;

public class TelegramWebhookControllerTests
{
    private List<TelegramBotConfigurations> botConfigurations;
    private Func<long, ITelegramBotService> telegramBotService;
    private readonly Fixture fixture = new();

    [Fact]
    public async Task SetWebhook_WhenCalled_ExpectCallExactFunction()
    {
        // Arrange
        var controller = GetController();
        var bot = botConfigurations.First();
        // Act
        await controller.SetWebhook();

        // Assert
        await telegramBotService(bot.BotId).Received(1).SetWebhookAsync(bot);
    }

    [Fact]
    public async Task SetWebhook_WhenHasError_ExpectReturnErrorMessage()
    {
        // Arrange
        var controller = GetController();
        var bot = botConfigurations.First();
        telegramBotService(bot.BotId).Returns(_ => throw new Exception("set webhook fail"));
        // Act
        var actual = await controller.SetWebhook();
        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as string;
        // Assert
        Assert.Equal("set webhook fail", actualResult);
    }

    [Fact]
    public async Task GetWebhookInfo_WhenCalled_ExpectReturnExactBotInfor()
    {
        // Arrange
        var controller = GetController();
        var bot = botConfigurations.First();
        var expectedWebhookInfo = fixture.Create<WebhookInfo>();
        telegramBotService(6012438440).GetWebhookInfoAsync(bot).Returns(expectedWebhookInfo);
        // Act
        var actual = await controller.GetWebhookInfo(6012438440);
        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as WebhookInfo;
        // Assert
        Assert.Equal(expectedWebhookInfo, actualResult);
        await telegramBotService(bot.BotId).Received(1).GetWebhookInfoAsync(bot);
    }

    [Fact]
    public async Task GetWebhookInfo_WhenDoNotFindBot_ExpectReturnErrorMessage()
    {
        // Arrange
        var controller = GetController();
        // Act
        var actual = await controller.GetWebhookInfo(1);
        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as string;
        // Assert
        Assert.Equal("bot does not exist.", actualResult);
    }

    [Fact]
    public async Task HealthCheck_WhenCalled_ExpectReturnBotStatus()
    {
        // Arrange
        var controller = GetController();
        var bot = botConfigurations.First();
        var expectedUser = fixture.Create<User>();
        telegramBotService(6012438440).GetBotInfoAsync(6012438440).Returns(expectedUser);
        // Act
        var actual = await controller.HealthCheck();
        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as string;
        // Assert
        Assert.Equal("bots are healthy!!!", actualResult);
        await telegramBotService(bot.BotId).Received(1).GetBotInfoAsync(6012438440);
    }

    [Fact]
    public async Task HealthCheck_WhenHasError_ExpectReturnErrorMessage()
    {
        // Arrange
        var controller = GetController();
        var bot = botConfigurations.First();
        telegramBotService(bot.BotId).Returns(_ => throw new("get bot info fail"));
        // Act
        var actual = await controller.HealthCheck();
        var okObjectResult = actual as OkObjectResult;
        var actualResult = okObjectResult?.Value as string;
        // Assert
        Assert.Equal("get bot info fail", actualResult);
    }

    #region helper

    private TelegramWebhookController GetController()
    {
        botConfigurations =
        [
            new()
            {
                BotId = 6012438440,
                Token = "Token",
                Domain = "Domain"
            }
        ];

        var botConfig = Substitute.For<List<TelegramBotConfigurations>>();
        botConfig.AddRange(botConfigurations);
        telegramBotService = Substitute.For<Func<long, ITelegramBotService>>();
        return new(
            botConfig,
            telegramBotService
        );
    }

    #endregion helper
}