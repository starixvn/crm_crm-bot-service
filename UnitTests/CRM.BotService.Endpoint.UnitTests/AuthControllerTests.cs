using CRM.BotService.Endpoint.Controllers;
using CRM.BotService.Endpoint.ViewModel;
using CRM.BotService.Models;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System;
using Xunit;

namespace CRM.BotService.Endpoint.Tests;

public class AuthControllerTests
{
    private readonly WebSettings webSettings;
    private readonly AuthController authController;

    public AuthControllerTests()
    {
        webSettings = Substitute.For<WebSettings>();
        authController = new AuthController(webSettings);
    }

    [Fact]
    public void Authenticate_ValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var authViewModel = new AuthViewModel
        {
            AppId = "validAppId",
            Token = Convert.ToBase64String("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ"u8.ToArray())
        };
        webSettings.AppId = "validAppId";
        webSettings.AppToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ";
        webSettings.Secret = "ieaTv1pLJ5iA3I4aZLc0uv7EMbrq15zK";
        webSettings.ExpirationByDay = 7;

        // Act
        var result = authController.Authenticate(authViewModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(okResult.Value);
        Assert.NotEmpty(jsonResponse);
        Assert.Contains("AccessToken", jsonResponse);
        Assert.Contains("ExpiredDate", jsonResponse);
    }

    [Fact]
    public void Authenticate_InvalidCredentials_ReturnsUnauthorizedResult()
    {
        // Arrange
        var authViewModel = new AuthViewModel
        {
            AppId = "invalidAppId",
            Token = "invalidToken"
        };
        webSettings.AppId = "validAppId";
        webSettings.AppToken = "validAppToken";

        // Act
        var result = authController.Authenticate(authViewModel);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
}