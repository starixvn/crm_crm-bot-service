using CRM.BotService.Endpoint.Controllers;
using CRM.BotService.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Endpoint.Tests;

public class HomeControllerTests
{
    private readonly ILogServiceExtend logServiceExtend;
    private readonly HomeController homeController;

    public HomeControllerTests()
    {
        logServiceExtend = Substitute.For<ILogServiceExtend>();
        homeController = new HomeController(logServiceExtend);
    }

    [Fact]
    public void Index_ReturnsOkResult()
    {
        // Act
        var result = homeController.Index();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("OK", okResult.Value);
    }

    [Fact]
    public async Task TestLogSentry_CallsLogServiceAndReturnsOkResult()
    {
        // Act
        var result = await homeController.TestLogSentry();

        // Assert
        await logServiceExtend.Received(1).ErrorMessageAsync("Test log sentry");
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Sentry test", okResult.Value);
    }
}