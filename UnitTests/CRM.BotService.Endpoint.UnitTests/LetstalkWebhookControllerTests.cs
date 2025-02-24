using CRM.BotService.Endpoint.Controllers;
using CRM.BotService.EventHandler;
using CRM.BotService.Models;
using CRM.BotService.Models.Letstalk.Result;
using CRM.BotService.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Endpoint.Tests;

public class LetstalkWebhookControllerTests
{
    private readonly ILetstalkEventHandler letstalkEventHandler;
    private readonly Func<long, ILetstalkClientService> letstalkService;
    private readonly LetstalkWebhookController controller;

    public LetstalkWebhookControllerTests()
    {
        letstalkEventHandler = Substitute.For<ILetstalkEventHandler>();
        letstalkService = Substitute.For<Func<long, ILetstalkClientService>>();
        List<WebhookConfiguration> webhookConfigurations1 = [
            new() { BotId = 1, IsAllowedGet = true, IsAllowedSet = true, WebhookBaseUrl = "abc.com/" },
            new() { BotId = 2, IsAllowedGet = false, IsAllowedSet = false, WebhookBaseUrl = "test.com/" }
        ];

        controller = new(letstalkEventHandler, letstalkService, webhookConfigurations1);
    }

    [Fact]
    public async Task WebhookReceiver_WhenCalledWithAnyParam_CallsExpectedWebhookReceiverService()
    {
        await controller.WebhookReceiver(1);
        controller.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext()
        };

        await letstalkEventHandler.Received().ReceiveAsync(Arg.Any<HttpRequest>(), Arg.Is(1));
    }

    [Fact]
    public async Task SetWebhook_WhenInCorrectBotId_ReturnNotFound()
    {
        var actual = await controller.SetWebhook(10);

        Assert.IsType<NotFoundResult>(actual);
    }

    [Fact]
    public async Task SetWebhook_WhenCorrectBotIdButConfigNotAllowSet_ReturnNotFound()
    {
        var actual = await controller.SetWebhook(2);

        Assert.IsType<NotFoundResult>(actual);
    }

    [Fact]
    public async Task SetWebhook_WhenCorrectBotIdAndConfigIsAllowSet_ReturnNotFound()
    {
        var result = new BaseResult { Status = 1, Message = "Success" };
        letstalkService(Arg.Any<long>()).SetWebhookAsync(Arg.Any<string>()).Returns(result);

        var actual = await controller.SetWebhook(1);

        await letstalkService(Arg.Any<long>()).SetWebhookAsync(Arg.Is("test.com/api/webhook/2"));
        var okResult = Assert.IsType<OkObjectResult>(actual);
        Assert.Equal(result, okResult.Value);
    }

    [Fact]
    public async Task GetWebhook_WhenInCorrectBotId_ReturnNotFound()
    {
        var actual = await controller.GetWebhook(10);

        Assert.IsType<NotFoundResult>(actual);
    }

    [Fact]
    public async Task GetWebhook_WhenCorrectBotIdButConfigNotAllowSet_ReturnNotFound()
    {
        var actual = await controller.GetWebhook(2);

        Assert.IsType<NotFoundResult>(actual);
    }

    [Fact]
    public async Task GetWebhook_WhenCorrectBotIdAndConfigIsAllowSet_ReturnNotFound()
    {
        var result = "webhook URL";
        letstalkService(Arg.Any<long>()).GetWebhookAsync().Returns(result);

        var actual = await controller.GetWebhook(1);

        await letstalkService(Arg.Any<long>()).GetWebhookAsync();
        var okResult = Assert.IsType<OkObjectResult>(actual);
        Assert.Equal(result, okResult.Value);
    }
}