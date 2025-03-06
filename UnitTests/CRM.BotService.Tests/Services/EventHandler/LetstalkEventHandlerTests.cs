using CRM.BotService.Constants;
using CRM.BotService.Criteria;
using CRM.BotService.EventHandler;
using CRM.BotService.Extensions;
using CRM.BotService.Models;
using CRM.BotService.Models.Letstalk;
using CRM.BotService.Models.Letstalk.BotConfig;
using CRM.BotService.Models.Letstalk.Result;
using CRM.BotService.Services;
using CRM.BotService.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Tests.Services.EventHandler;

public class LetstalkEventHandlerTests
{
    private IBaseService baseService;
    private ILogServiceExtend crMLogService;
    private Func<long, ILetstalkClientService> letstalkClient;
    private const string FakeSecurekey = "nO8isnuBjeQcflwU81VyQv5prWpnNZP4NUJI3rnxiBDJ9M96l9vjSti4gIkLsi0aelgvHzHnTarn2pVet6BkJsC9DwUCBkZqJ7d1z7BkxrjbNYw7x9yX952dvAjmsx7T";

    [Fact]
    public async Task ReceiveAsync_WhenJoinGroup_CallExpectedServiceAsync()
    {
        var service = GetService();
        letstalkClient(1).GetChatRoomData(Arg.Is("groupId")).Returns(new RoomData { Title = "GroupName" });
        baseService.ExecuteNonQueryAsync(Arg.Any<AddBotToGroupCommand>()).Returns(new CommandResult { Code = 1 });
        var httpRequest = CreateRequest(EventType.JoinGroup);

        await service.ReceiveAsync(httpRequest, 1);

        await letstalkClient(1).Received().GetChatRoomData(Arg.Is("groupId"));
        await letstalkClient(1).Received().PushRoomAsync(Arg.Is("groupId"), Arg.Any<OutgoingMessage>());
        await baseService.Received().ExecuteNonQueryAsync(Arg.Any<AddBotToGroupCommand>());
        await crMLogService.Received().ErrorAsync(Arg.Any<Exception>());
    }

    [Fact]
    public async Task ReceiveAsync_WhenKickOutGroup_CallExpectedServiceAsync()
    {
        var service = GetService();
        baseService.ExecuteNonQueryAsync(Arg.Any<RemoveBotFromGroupCommand>()).Returns(new CommandResult { Code = 1 });
        var httpRequest = CreateRequest(EventType.KickOutGroup);

        await service.ReceiveAsync(httpRequest, 1);

        await letstalkClient(1).DidNotReceive().GetChatRoomData(Arg.Is("groupId"));
        await baseService.Received().ExecuteNonQueryAsync(Arg.Any<RemoveBotFromGroupCommand>());
        await crMLogService.Received().ErrorAsync(Arg.Any<Exception>());
    }

    [Fact]
    public async Task ReceiveAsync_WhenChangeGroupName_CallExpectedServiceAsync()
    {
        var service = GetService();
        letstalkClient(1).GetChatRoomData(Arg.Is("groupId")).Returns(new RoomData { Title = "GroupName" });
        baseService.ExecuteNonQueryAsync(Arg.Any<UpdateGroupNameCommand>()).Returns(new CommandResult { Code = 1 });
        var httpRequest = CreateRequest(EventType.ChangeGroupName);

        await service.ReceiveAsync(httpRequest, 1);

        await letstalkClient(1).Received().GetChatRoomData(Arg.Is("groupId"));
        await baseService.Received().ExecuteNonQueryAsync(Arg.Any<UpdateGroupNameCommand>());
        await crMLogService.Received().ErrorAsync(Arg.Any<Exception>());
    }

    [Fact]
    public async Task ReceiveAsync_AddContact_CallExpectedServiceAsync()
    {
        var service = GetService();
        var httpRequest = CreateRequest(EventType.AddContact);

        await service.ReceiveAsync(httpRequest, 1);

        await letstalkClient(1).Received().PushPersonalAsync(Arg.Is("letstalkId"), Arg.Any<OutgoingMessage>());
    }

    [Fact]
    public async Task ReceiveAsync_RemoveContact_DoNothing()
    {
        var service = GetService();
        var httpRequest = CreateRequest(EventType.DeleteContact);

        await service.ReceiveAsync(httpRequest, 1);

        await letstalkClient(Arg.Any<long>()).DidNotReceive().PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
    }

    [Fact]
    public async Task ReceiveAsync_ReceiveMessage_PushMessagBack()
    {
        var service = GetService();
        var httpRequest = CreateRequest(EventType.Message);

        await service.ReceiveAsync(httpRequest, 1);

        await letstalkClient(1).Received().PushPersonalAsync(Arg.Is("letstalkId"), Arg.Any<OutgoingMessage>());
    }

    private HttpRequest CreateRequest(string type)
    {
        var hookEvent = new Dictionary<string, object>
        {
            { "Source", new { group_id = "groupId", user_id = "letstalkId" } },
            { "Payload", string.Empty },
            {"Type", type }
        };
        var encoded = JwtHelper.Encode(FakeSecurekey, hookEvent);
        var request = Substitute.For<HttpRequest>();
        request.Body.Returns(GenerateStreamFromString(encoded));
        return request;
    }

    private static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private LetstalkEventHandler GetService()
    {
        var botConfig = new List<LetstalkBotConfig>
        {
            new()
            {
                BotAccountId = 1,
                SecureKey = FakeSecurekey,
                DisplayName = "BotName"
            }
        };
        baseService = Substitute.For<IBaseService>();
        crMLogService = Substitute.For<ILogServiceExtend>();
        letstalkClient = Substitute.For<Func<long, ILetstalkClientService>>();
        var service = new LetstalkEventHandler(baseService, crMLogService, letstalkClient, botConfig);
        return service;
    }
}