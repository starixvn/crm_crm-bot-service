using CRM.BotService.Criteria;
using CRM.BotService.Endpoint.Controllers;
using CRM.BotService.Endpoint.ViewModel;
using CRM.BotService.Enum;
using CRM.BotService.Models.Base;
using CRM.BotService.Models.Letstalk;
using CRM.BotService.Models.Letstalk.Result;
using CRM.BotService.Services;
using CRM.BotService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Endpoint.Tests;

public class LetstalkControllerTests
{
    private Func<long, ILetstalkClientService> letstalkClientService;
    private IBotCommonService botCommonService;
    private ILogServiceExtend logService;
    private ILetstalkSendMultipleReceiverService letstalkSendMultipleReceiverService;

    [Fact]
    public async Task CheckPersonalProfileAsync_WhenCalled_ReturnOkObjectResult()
    {
        var controller = GetController();
        var profile = new Contact { UserId = "oliver.dang", DisplayName = "Oliver", AvatarUrl = "url" };
        letstalkClientService(Arg.Any<long>()).GetProfileAsync(Arg.Any<string>()).Returns(profile);

        var actual = await controller.CheckPersonalProfileAsync(new() { BotId = 1, LetstalkId = "1234aa" });

        var okObjectResult = Assert.IsType<OkObjectResult>(actual);
        Assert.Equal(JsonConvert.SerializeObject(profile), JsonConvert.SerializeObject(okObjectResult.Value));
    }

    [Fact]
    public async Task VerifyUserOnGroupAsync_WhenBotRemoved_ReturnEmptyListLetstalk()
    {
        var controller = GetController();
        var roomList = new List<string> { "group1", "group2", "group3" };
        letstalkClientService(1).GetChatRoomList().Returns(roomList);
        var expected = JsonConvert.SerializeObject(new
        {
            IsBotRemoved = true,
            Letstalks = new List<string>()
        });

        var actual = await controller.VerifyUserOnGroupAsync(new() { BotId = 1, ConversationGroupId = "group4" });

        var okObjectResult = Assert.IsType<OkObjectResult>(actual);
        Assert.Equal(200, okObjectResult.StatusCode);
        var json = JsonConvert.SerializeObject(okObjectResult.Value);
        Assert.Equal(expected, json);
        await letstalkClientService(Arg.Any<long>()).DidNotReceive().GetBotContactList();
        await letstalkClientService(Arg.Any<long>()).DidNotReceive().GetChatRoomData(Arg.Any<string>());
    }

    [Fact]
    public async Task VerifyUserOnGroupAsync_WhenBotHaveNotRemoved_ReturnExpectedResult()
    {
        var controller = GetController();
        var roomList = new List<string> { "group1" };
        var roomData = new RoomData { Title = "Group 1 name", Members = ["member1", "member2"] };
        var botFriends = new List<string> { "member1" };
        letstalkClientService(1).GetChatRoomList().Returns(roomList);
        letstalkClientService(1).GetBotContactList().Returns(botFriends);
        letstalkClientService(1).GetChatRoomData("group1").Returns(roomData);

        var param = new VerifyUserOnGroupViewModel
        {
            BotId = 1,
            ConversationGroupId = "group1",
            BotAccountIds = ["member1", "member2", "member3"]
        };
        var expected = new
        {
            IsBotRemoved = false,
            Letstalks = new List<object>
            {
                new { LetstalkId = "member1", IsGroupMember = true, IsBotFriend = true },
                new { LetstalkId = "member2", IsGroupMember = true, IsBotFriend = false },
                new { LetstalkId = "member3", IsGroupMember = false, IsBotFriend = false }
            }
        };

        var actual = await controller.VerifyUserOnGroupAsync(param);

        var okObjectResult = Assert.IsType<OkObjectResult>(actual);
        var resultData = JsonConvert.SerializeObject(okObjectResult.Value);
        Assert.Equal(JsonConvert.SerializeObject(expected), resultData);
    }

    [Fact]
    public async Task SendMultipleMessage_WhenInputEmptyList_ReturnEmptyResult()
    {
        var controller = GetController();

        var action = await controller.SendPlainTextMultipleMessage([]);

        var actual = Assert.IsType<OkObjectResult>(action);
        var resultData = actual.Value as List<SendResult>;
        Assert.NotNull(resultData);
        Assert.Empty(resultData);
    }

    [Fact]
    public async Task SendMultipleMessage_WhenInputHaveData_ActivedChannel_SendAllSuccess_ReturnExpectedResult()
    {
        var input = FakeInput();
        var controller = GetController();
        letstalkClientService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>())
            .Returns(new PushMessageResult { Status = 1 });
        letstalkClientService(Arg.Any<long>()).PushRoomAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>())
            .Returns(new PushMessageResult { Status = 1 });
        botCommonService.IsActiveChannel(Arg.Any<BotChannel>()).Returns(true);

        var action = await controller.SendPlainTextMultipleMessage(input);

        var actual = Assert.IsType<OkObjectResult>(action);
        Assert.Equal(200, actual.StatusCode);
        var resultData = actual.Value as List<SendResult>;
        Assert.NotNull(resultData);
        Assert.Equal(5, resultData.Count);
        await letstalkClientService(Arg.Any<long>()).Received(3)
            .PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await letstalkClientService(Arg.Any<long>()).Received(2)
            .PushRoomAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await botCommonService.Received(5).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
    }

    [Fact]
    public async Task
        SendMultipleMessage_WhenInputHaveData_ActivedChannel_SendToPersonalThrowsException_StillReturnExpectedResult()
    {
        var input = FakeInput();
        var controller = GetController();
        letstalkClientService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>())
            .Throws<Exception>();
        letstalkClientService(Arg.Any<long>()).PushRoomAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>())
            .Returns(new PushMessageResult { Status = 1 });
        botCommonService.IsActiveChannel(Arg.Any<BotChannel>()).Returns(true);

        var action = await controller.SendPlainTextMultipleMessage(input);

        var actual = Assert.IsType<OkObjectResult>(action);
        Assert.Equal(200, actual.StatusCode);
        var resultData = actual.Value as List<SendResult>;
        Assert.NotNull(resultData);
        Assert.Equal(5, resultData.Count);

        await letstalkClientService(Arg.Any<long>()).Received(3)
            .PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await letstalkClientService(Arg.Any<long>()).Received(2)
            .PushRoomAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await logService.Received(3).ErrorAsync(Arg.Any<Exception>());
        await botCommonService.Received(5).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
    }

    [Fact]
    public async Task SendMultipleMessage_WhenInputHaveData_InActivedChannel_NoCallSendToLetstalk()
    {
        var input = FakeInput();
        var controller = GetController();

        botCommonService.IsActiveChannel(Arg.Any<BotChannel>()).Returns(false);

        var action = await controller.SendPlainTextMultipleMessage(input);

        var actual = Assert.IsType<OkObjectResult>(action);
        Assert.Equal(200, actual.StatusCode);
        var resultData = actual.Value as List<SendResult>;
        Assert.NotNull(resultData);
        Assert.Equal(5, resultData.Count);
        var noSendSuccessResult = resultData.Any(x => x.IsSuccessful);
        Assert.False(noSendSuccessResult);
        await letstalkClientService(Arg.Any<long>()).DidNotReceive()
            .PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await letstalkClientService(Arg.Any<long>()).DidNotReceive()
            .PushRoomAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
    }

    [Fact]
    public async Task SendSingleMessage_WhenCalled_ReturnExpectedResult()
    {
        var controller = GetController();
        botCommonService.IsActiveChannel(Arg.Any<BotChannel>()).Returns(true);
        letstalkClientService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>())
            .Returns(new PushMessageResult { Status = 1, BlockList = [], Message = string.Empty });

        var action = await controller.SendSingleMessage(new()
        {
            NotificationId = 1,
            ConversationGroupId = 1,
            BotId = 1,
            ChatTypeId = 2,
            ChannelId = BotChannel.Letstalk,
            Content = "abc",
            ConversationId = "abc"
        });

        var actual = Assert.IsType<OkObjectResult>(action);
        var resultData = actual.Value as SendResult;
        Assert.NotNull(resultData);
        Assert.True(resultData.IsSuccessful);
        Assert.False(resultData.IsBlockedBot);
        await botCommonService.Received(1).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
    }

    [Fact]
    public async Task SendSingleMessage_ToPersonal_WhenCalledWithSendImage_ReturnExpectedResult()
    {
        var controller = GetController();
        botCommonService.IsActiveChannel(Arg.Any<BotChannel>()).Returns(true);
        letstalkClientService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>())
            .Returns(new PushMessageResult { Status = 1, BlockList = [], Message = string.Empty });

        var action = await controller.SendSingleMessage(new()
        {
            NotificationId = 1,
            ConversationGroupId = 1,
            BotId = 1,
            ChatTypeId = 2,
            ChannelId = BotChannel.Letstalk,
            Content = "abc",
            ConversationId = "abc"
        });

        var actual = Assert.IsType<OkObjectResult>(action);
        var resultData = actual.Value as SendResult;
        Assert.NotNull(resultData);
        Assert.True(resultData.IsSuccessful);
        Assert.False(resultData.IsBlockedBot);
        await letstalkClientService(Arg.Any<long>()).Received(1)
            .PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await letstalkClientService(Arg.Any<long>()).DidNotReceive()
            .PushRoomAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await botCommonService.Received(1).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
    }

    [Fact]
    public async Task SendSingleMessage_ToGroup_WhenCalledWithSendImage_ReturnExpectedResult()
    {
        var controller = GetController();
        botCommonService.IsActiveChannel(Arg.Any<BotChannel>()).Returns(true);
        letstalkClientService(Arg.Any<long>()).PushRoomAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>())
            .Returns(new PushMessageResult { Status = 1, BlockList = [], Message = string.Empty });

        var action = await controller.SendSingleMessage(new()
        {
            NotificationId = 1,
            ConversationGroupId = 1,
            BotId = 1,
            ChatTypeId = 1,
            ChannelId = BotChannel.Letstalk,
            Content = "abc",
            ConversationId = "abc"
        });

        var actual = Assert.IsType<OkObjectResult>(action);
        var resultData = actual.Value as SendResult;

        Assert.NotNull(resultData);
        Assert.True(resultData.IsSuccessful);
        Assert.False(resultData.IsBlockedBot);
        await letstalkClientService(Arg.Any<long>()).Received(1)
            .PushRoomAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await letstalkClientService(Arg.Any<long>()).DidNotReceive()
            .PushPersonalAsync(Arg.Any<string>(), Arg.Any<OutgoingMessage>());
        await botCommonService.Received(1).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
    }

    #region Helper

    private List<NotificationMessage> FakeInput()
    {
        return
        [
            new() { ChatTypeId = 1, BotId = 1, ConversationId = "abc", NotificationId = 1, Content = "" },
            new() { ChatTypeId = 1, BotId = 1, ConversationId = "xyz", NotificationId = 2, Content = "" },
            new() { ChatTypeId = 2, BotId = 1, ConversationId = "def", NotificationId = 3, Content = "" },
            new() { ChatTypeId = 2, BotId = 1, ConversationId = "ghi", NotificationId = 4, Content = "" },
            new() { ChatTypeId = 2, BotId = 1, ConversationId = "jkm", NotificationId = 5, Content = "" }
        ];
    }

    private LetstalkController GetController()
    {
        letstalkClientService = Substitute.For<Func<long, ILetstalkClientService>>();
        botCommonService = Substitute.For<IBotCommonService>();
        logService = Substitute.For<ILogServiceExtend>();
        letstalkSendMultipleReceiverService = Substitute.For<ILetstalkSendMultipleReceiverService>();
        return new(letstalkClientService, botCommonService, logService,
            letstalkSendMultipleReceiverService);
    }

    #endregion Helper
}