using AutoFixture;
using CRM.BotService.Enum;
using CRM.BotService.Models.Letstalk.BotConfig;
using CRM.BotService.Models.Letstalk.Result;
using CRM.BotService.Services;
using CRM.BotService.Tests.Helpers;
using Fanex.Caching;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Tests.Services;

public class LetstalkClientServiceTests
{
    private const string LetstalkCacheKey = "CRM-Letstalk-API-Token";
    private readonly Fixture fixture = new();
    private LetstalkBotConfig botConfig;
    private IHttpClientFactory clientFactory;
    private ICacheService cacheService;

    [Fact]
    public async Task GetBotContactList_WhenGetSuccessStatus_ReturnExpectedResult()
    {
        var service = GetService();
        var expectedResult = new BotContactListResult
        {
            Status = 1,
            Message = string.Empty,
            Contacts = fixture.Create<List<string>>()
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetBotContactList();
        Assert.Equal(JsonConvert.SerializeObject(expectedResult.Contacts), JsonConvert.SerializeObject(actual));
    }

    [Fact]
    public async Task GetBotContactList_WhenGetFailStatus_ReturnListContact()
    {
        var service = GetService();
        var expectedResult = new BotContactListResult
        {
            Status = 0,
            Message = string.Empty,
            Contacts = fixture.Create<List<string>>()
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetBotContactList();
        Assert.Empty(actual);
    }

    [Fact]
    public async Task GetChatRoomData_WhenGetSuccessStatus_ReturnExpectedResult()
    {
        var service = GetService();
        var expectedResult = new RoomResult
        {
            Status = 1,
            Message = string.Empty,
            Room = fixture.Create<RoomData>()
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetChatRoomData("room");
        Assert.Equal(JsonConvert.SerializeObject(expectedResult.Room), JsonConvert.SerializeObject(actual));
    }

    [Fact]
    public async Task GetChatRoomData_WhenGetFailStatus_ReturnListContact()
    {
        var service = GetService();
        var expectedResult = new RoomResult
        {
            Status = 0,
            Message = string.Empty,
            Room = fixture.Create<RoomData>()
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetChatRoomData("room");
        Assert.Null(actual);
    }

    [Fact]
    public async Task GetProfileAsync_WhenGetSuccessStatus_ReturnExpectedResult()
    {
        var service = GetService();
        var expectedResult = new ProfileResult
        {
            Status = 1,
            Message = string.Empty,
            DisplayName = fixture.Create<string>(),
            AvatarUrl = fixture.Create<string>()
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetProfileAsync("room");
        Assert.Equal(expectedResult.DisplayName, actual.DisplayName);
        Assert.Equal(expectedResult.AvatarUrl, actual.AvatarUrl);
        Assert.Equal("room", actual.UserId);
    }

    [Fact]
    public async Task GetProfileAsync_WhenGetFailStatus_ReturnListContact()
    {
        var service = GetService();
        var expectedResult = new ProfileResult
        {
            Status = 0,
            Message = string.Empty,
            DisplayName = fixture.Create<string>(),
            AvatarUrl = fixture.Create<string>()
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetProfileAsync("room");
        Assert.Null(actual);
    }

    [Fact]
    public async Task PushRoomAsync_WhenInvalidMulticastInput_ThrowsException()
    {
        var service = GetService();
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.PushRoomAsync("accountId", new()
        {
            MessageType = NotificationMessageType.PlainText,
            Text = string.Empty
        }));

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.PushRoomAsync("accountId", new()
        {
            MessageType = NotificationMessageType.Html,
            Text = string.Empty
        }));

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.PushRoomAsync("accountId", new()
        {
            MessageType = NotificationMessageType.Image,
            ImageUrl = string.Empty
        }));

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.PushRoomAsync("accountId", new()
        {
            MessageType = NotificationMessageType.FullImage,
            ImageUrl = string.Empty
        }));
    }

    [Fact]
    public async Task PushRoomAsync_WhenValidInput_WhenNoCachedToken_WillCallApiGetTokenResult()
    {
        var service = GetService();
        cacheService.GetAsync<string>(Arg.Any<string>()).Returns(string.Empty);

        var tokenResult = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(
                new TokenResult
                {
                    Status = 1,
                    Message = string.Empty,
                    Token = "token"
                }))
        };

        var pushRoomResult = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(
                new PushMessageResult
                {
                    Status = 1,
                    Message = string.Empty
                }))
        };
        MockHttpClientMultiple(new Dictionary<string, HttpResponseMessage>
        {
            { botConfig.ApiEndpoint + "bot/v3/sign-token", tokenResult },
            { botConfig.ApiEndpoint + "bot/v3/message/multi-chatroom", pushRoomResult }
        });
        await service.PushRoomAsync("account",
            new() { MessageType = NotificationMessageType.Html, Text = "Content" });

        await cacheService.Received().GetAsync<string>(Arg.Is(LetstalkCacheKey));
        await cacheService.Received().SetAsync(Arg.Is(LetstalkCacheKey), Arg.Is("token"), Arg.Any<CacheItemOptions>());
    }

    [Fact]
    public async Task PushRoomAsync_WhenValidInput_WhenHadCachedToken_WillNotCallApiGetTokenAndSetCache()
    {
        var service = GetService();
        cacheService.GetAsync<string>(Arg.Any<string>()).Returns("token");

        var tokenResult = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(
                new TokenResult
                {
                    Status = 1,
                    Message = string.Empty,
                    Token = "token"
                }))
        };

        var pushRoomResult = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(
                new PushMessageResult
                {
                    Status = 1,
                    Message = string.Empty
                }))
        };
        MockHttpClientMultiple(new Dictionary<string, HttpResponseMessage>
        {
            { botConfig.ApiEndpoint + "bot/v3/sign-token", tokenResult },
            { botConfig.ApiEndpoint + "bot/v3/message/multi-chatroom", pushRoomResult }
        });
        var actual = await service.PushRoomAsync("account",
            new() { MessageType = NotificationMessageType.Html, Text = "Content" });

        await cacheService.Received(1).GetAsync<string>(Arg.Any<string>());
        await cacheService.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CacheItemOptions>());
        clientFactory.Received(1).CreateClient(Arg.Any<string>());
        Assert.True(actual.IsSuccess);
        Assert.Equal(1, actual.Status);
        Assert.Empty(actual.Message);
    }

    [Fact]
    public async Task SetWebhookAsync_WhenCalled_ReturnExpectedResult()
    {
        var service = GetService();
        var expectedResult = new BaseResult
        {
            Status = 1,
            Message = string.Empty
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.SetWebhookAsync("url");
        Assert.True(actual.IsSuccess);
    }

    [Fact]
    public async Task GetChatRoomList_WhenGetSuccessStatus_ReturnExpectedResult()
    {
        var service = GetService();
        var expectedResult = new BotChatRoomListResult
        {
            Status = 1,
            Message = string.Empty,
            Rooms = fixture.Create<List<string>>()
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetChatRoomList();
        Assert.Equal(JsonConvert.SerializeObject(expectedResult.Rooms), JsonConvert.SerializeObject(actual));
    }

    [Fact]
    public async Task GetChatRoomList_WhenGetFailStatus_ReturnListContact()
    {
        var service = GetService();
        var expectedResult = new BotContactListResult
        {
            Status = 0,
            Message = string.Empty,
            Contacts = fixture.Create<List<string>>()
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetChatRoomList();
        Assert.Equal(0, actual.Count);
    }

    [Fact]
    public async Task GetWebhookAsync_WhenSuccessResult_ReturnExpectedResult()
    {
        var service = GetService();
        var expectedResult = new WebhookResult
        {
            Status = 1,
            Message = string.Empty,
            Url = "aaa"
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetWebhookAsync();
        Assert.Equal("aaa", actual);
    }

    [Fact]
    public async Task GetWebhookAsync_WhenFailedResult_ReturnEmptyString()
    {
        var service = GetService();
        var expectedResult = new WebhookResult
        {
            Status = 0,
            Message = string.Empty,
            Url = "aaa"
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedResult))
        };
        MockHttpClient(response);
        var actual = await service.GetWebhookAsync();
        Assert.Empty(actual);
    }

    [Fact]
    public async Task PushPersonalAsync_WhenValidInput_WhenNoCachedToken_WillCallApiGetTokenResult()
    {
        var service = GetService();
        cacheService.GetAsync<string>(Arg.Any<string>()).Returns(string.Empty);

        var tokenResult = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(
                new TokenResult
                {
                    Status = 1,
                    Message = string.Empty,
                    Token = "token"
                }))
        };

        var pushRoomResult = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(
                new PushMessageResult
                {
                    Status = 1,
                    Message = string.Empty
                }))
        };
        MockHttpClientMultiple(new Dictionary<string, HttpResponseMessage>
        {
            { botConfig.ApiEndpoint + "bot/v3/sign-token", tokenResult },
            { botConfig.ApiEndpoint + "bot/v3/message/multicast", pushRoomResult }
        });
        await service.PushPersonalAsync("account",
            new() { MessageType = NotificationMessageType.Html, Text = "Content" });

        await cacheService.Received().GetAsync<string>(Arg.Is(LetstalkCacheKey));
        await cacheService.Received().SetAsync(Arg.Is(LetstalkCacheKey), Arg.Is("token"), Arg.Any<CacheItemOptions>());
    }

    [Fact]
    public async Task PushPersonalAsync_WhenValidInput_WhenHadCachedToken_WillNotCallApiGetTokenAndSetCache()
    {
        var service = GetService();
        cacheService.GetAsync<string>(Arg.Any<string>()).Returns("token");

        var tokenResult = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(
                new TokenResult
                {
                    Status = 1,
                    Message = string.Empty,
                    Token = "token"
                }))
        };

        var pushRoomResult = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(
                new PushMessageResult
                {
                    Status = 1,
                    Message = string.Empty
                }))
        };
        MockHttpClientMultiple(new Dictionary<string, HttpResponseMessage>
        {
            { botConfig.ApiEndpoint + "bot/v3/sign-token", tokenResult },
            { botConfig.ApiEndpoint + "bot/v3/message/multicast", pushRoomResult }
        });
        var actual = await service.PushPersonalAsync("account",
            new() { MessageType = NotificationMessageType.Html, Text = "Content" });

        await cacheService.Received(1).GetAsync<string>(Arg.Any<string>());
        await cacheService.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CacheItemOptions>());
        clientFactory.Received(1).CreateClient(Arg.Any<string>());
        Assert.True(actual.IsSuccess);
        Assert.Equal(1, actual.Status);
        Assert.Empty(actual.Message);
    }

    #region Helper

    private void MockHttpClient(HttpResponseMessage resMsg)
    {
        clientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new Helpers.MockHttpMessageHandler(resMsg)));
    }

    private void MockHttpClientMultiple(IDictionary<string, HttpResponseMessage> messages)
    {
        clientFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new MockHttpMessageHandlerMultiple(messages)));
    }

    private LetstalkClientService GetService()
    {
        botConfig = new()
        {
            ApiEndpoint = "http://test.com",
            ApiKey = "Key",
            BotAccountId = 1000,
            BotUserId = "@test",
            BusinessAccountType = 1,
            ChatKey = "ABC",
            ChatKeyVersion = 5,
            EncryptionMode = 2,
            SecureKey = "Secure"
        };
        cacheService = Substitute.For<ICacheService>();
        clientFactory = Substitute.For<IHttpClientFactory>();
        return new(clientFactory, cacheService, botConfig);
    }

    #endregion Helper
}