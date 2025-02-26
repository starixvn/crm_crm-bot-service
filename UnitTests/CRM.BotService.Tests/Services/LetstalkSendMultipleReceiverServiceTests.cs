using CRM.BotService.Criteria;
using CRM.BotService.Enum;
using CRM.BotService.Models.Letstalk;
using CRM.BotService.Models.Letstalk.Result;
using CRM.BotService.Models.Notification;
using CRM.BotService.Services;
using CRM.BotService.Services.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Tests.Services;

public class LetstalkSendMultipleReceiverServiceTests
{
    private ILogServiceExtend logService;
    private IBotCommonService botCommonService;
    private Func<long, ILetstalkClientService> letstalkService;

    [Fact]
    public async Task SendFornattedTextAndImagesAsync_SendSuccessContenAndImage_WillCallExpectedServices()
    {
        var service = GetService();
        var data = FakeNotification();

        letstalkService(Arg.Any<long>()).PushRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Returns(new PushMessageResult { Status = 1 });
        letstalkService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Returns(new PushMessageResult { Status = 1 });

        //Actual
        var actual = await service.SendFormattedTextAndImagesAsync(data);

        //Expected
        await botCommonService.Received(8).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());

        Assert.Equal(data.Count, actual.Count());
    }

    [Fact]
    public async Task SendFormattedTextAndImagesAsync_SendImageThrowsException_ReturnExpectedResult()
    {
        var service = GetService();
        var data = FakeNotification();

        letstalkService(Arg.Any<long>()).PushRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == NotificationMessageType.Html)).Returns(new PushMessageResult { Status = 1 });
        letstalkService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == NotificationMessageType.Html)).Returns(new PushMessageResult { Status = 1 });

        letstalkService(Arg.Any<long>()).PushRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == NotificationMessageType.Image)).Throws(new Exception());
        letstalkService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == NotificationMessageType.Image)).Throws(new Exception());

        //Actual
        var actual = await service.SendFormattedTextAndImagesAsync(data);

        //Expected
        await botCommonService.Received().WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());

        Assert.NotNull(actual);
        var sendNotificationResults = actual as SendNotificationResult[] ?? actual.ToArray();
        Assert.Equal(data.Count, sendNotificationResults.Length);
        Assert.Contains(sendNotificationResults, i => !i.IsSuccessful);
    }

    [Fact]
    public async Task SendFormattedTextAndImagesAsync_SendLetstalkThrowException_StillReturnExpectedResult()
    {
        var service = GetService();
        var data = FakeNotification();

        letstalkService(Arg.Any<long>()).PushRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Throws(new Exception());
        letstalkService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Throws(new Exception());
        //Actual
        var actual = await service.SendFormattedTextAndImagesAsync(data);

        //Expected
        await botCommonService.Received(4).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
        await letstalkService(Arg.Any<long>()).DidNotReceive().PushRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == NotificationMessageType.Image));
        await letstalkService(Arg.Any<long>()).DidNotReceive().PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == NotificationMessageType.Image));

        var sendNotificationResults = actual as SendNotificationResult[] ?? actual.ToArray();
        Assert.Equal(data.Count, sendNotificationResults.Length);
        Assert.Contains(sendNotificationResults, i => !i.IsSuccessful);
    }

    [Fact]
    public async Task SendImageAsync_WhenCalled_DoNotCallSendHtmlContent()
    {
        var service = GetService();
        var data = FakeNotification();

        letstalkService(Arg.Any<long>()).PushRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Throws(new Exception());
        letstalkService(Arg.Any<long>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Throws(new Exception());
        //Actual
        var actual = await service.SendImageAsync(data);

        //Expected
        await botCommonService.Received(4).WriteLogMessageAsync(Arg.Any<WriteLogMessageCommand>());
        await letstalkService(Arg.Any<long>()).DidNotReceive().PushRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == NotificationMessageType.Html));
        await letstalkService(Arg.Any<long>()).DidNotReceive().PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == NotificationMessageType.Html));

        var sendNotificationResults = actual as SendNotificationResult[] ?? actual.ToArray();
        Assert.Equal(data.Count, sendNotificationResults.Length);
        Assert.Contains(sendNotificationResults, i => !i.IsSuccessful);
    }

    private static List<AnnouncementNotification> FakeNotification()
        =>
        [
            new()
            {
                BotId = 1, AnnouncementId = 1, ChatTypeId = ChatType.Group, Content = "1", Images = ["1"],
                ConversationGroupId = 1, ConversationId = "abc", ErrImages = "1", RetryImages = ["1"]
            },
            new()
            {
                BotId = 1, AnnouncementId = 2, ChatTypeId = ChatType.Group, Content = "1", Images = ["1"],
                ConversationGroupId = 2, ConversationId = "def", ErrImages = "1", RetryImages = ["1"]
            },
            new()
            {
                BotId = 1, AnnouncementId = 1, ChatTypeId = ChatType.Personal, Content = "1", Images = ["1"],
                ConversationGroupId = 3, ConversationId = "agh", ErrImages = "1", RetryImages = ["1"]
            },
            new()
            {
                BotId = 1, AnnouncementId = 1, ChatTypeId = ChatType.Personal, Content = "1", Images = ["1"],
                ConversationGroupId = 4, ConversationId = "asc", ErrImages = "1", RetryImages = ["1"]
            },
            new()
            {
                BotId = 1, AnnouncementId = 2, ChatTypeId = ChatType.Personal, Content = "1", Images = ["1"],
                ConversationGroupId = 5, ConversationId = "uhj", ErrImages = "1", RetryImages = ["1"]
            },
            new()
            {
                BotId = 1, AnnouncementId = 1, ChatTypeId = ChatType.Personal, Content = "1", Images = ["1"],
                ConversationGroupId = 6, ConversationId = "1bcs", ErrImages = "1", RetryImages = ["1"]
            }
        ];

    private LetstalkSendMultipleReceiverService GetService()
    {
        logService = Substitute.For<ILogServiceExtend>();
        botCommonService = Substitute.For<IBotCommonService>();
        letstalkService = Substitute.For<Func<long, ILetstalkClientService>>();
        return new(botCommonService, logService, letstalkService);
    }
}