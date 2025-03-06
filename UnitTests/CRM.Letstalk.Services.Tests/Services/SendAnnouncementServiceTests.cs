namespace CRM.Letstalk.Services.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CRM.Core.Services;
    using CRM.Letstalk.Client.Enum;
    using CRM.Letstalk.Client.Models;
    using CRM.Letstalk.Client.Models.Results;
    using CRM.Letstalk.Client.Services.Interfaces;
    using CRM.Letstalk.Services.Criteria;
    using CRM.Letstalk.Services.Models.SendAnnouncement;
    using CRM.Letstalk.Services.Services;
    using Microsoft.Extensions.Configuration;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;
    using Xunit;

    public class SendAnnouncementServiceTests
    {
        private readonly SendAnnouncementService service;
        private readonly IBaseService baseService;
        private readonly IConfiguration configuration;
        private readonly ICRMLogService logService;
        private readonly Func<int, ILetstalkService> letstalkService;

        public SendAnnouncementServiceTests()
        {
            letstalkService = Substitute.For<Func<int, ILetstalkService>>();
            configuration = Substitute.For<IConfiguration>();
            baseService = Substitute.For<IBaseService>();
            logService = Substitute.For<ICRMLogService>();
            service = new SendAnnouncementService(baseService, logService, configuration, letstalkService);
        }

        [Fact]
        public async Task ScanThenSendAsnyc_WhenNoData_DoNothing()
        {
            baseService.GetDataAsync<AnnouncementNotification>(Arg.Any<GetAnnouncementNotificationToSendCriteria>()).Returns(new List<AnnouncementNotification>());
            await service.ScanThenSendAsnyc();
            await logService.DidNotReceive().ErrorAsync(Arg.Any<Exception>());
            await letstalkService(Arg.Any<int>()).DidNotReceive().PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
            await letstalkService(Arg.Any<int>()).DidNotReceive().PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
        }

        [Fact]
        public async Task ScanThenSendAsnyc_OnlyHaveBotRemoved_UpdateStatus_CallExpectedServices()
        {
            //Arrange
            var fakeNotification = new List<AnnouncementNotification>
            {
                new AnnouncementNotification {BotId = 0, AnnouncementId = 1, ChatTypeId = 1},
                new AnnouncementNotification {BotId = 0, AnnouncementId = 2, ChatTypeId = 1},
            };
            baseService.GetDataAsync<AnnouncementNotification>(Arg.Any<GetAnnouncementNotificationToSendCriteria>()).Returns(fakeNotification);

            //Actual
            await service.ScanThenSendAsnyc();

            //Expected
            await baseService.Received(1).ExecuteNonQueryAsync(Arg.Any<UpdateAnnouncementNotificationStatusCommand>());
            await letstalkService(Arg.Any<int>()).DidNotReceive().PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
            await letstalkService(Arg.Any<int>()).DidNotReceive().PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
        }

        [Fact]
        public async Task ScanThenSendAsnyc_OnlyHaveRetryOnlyImage_WillCallExpectedService()
        {
            //Arrange
            var fakeNotification = new List<AnnouncementNotification>
            {
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 1, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =1, ConversationId ="abc"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 1, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =2, ConversationId ="def"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 1, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =3, ConversationId ="agh"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 2, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =4, ConversationId ="asc"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 2, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =5, ConversationId ="uhj"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 2, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =6, ConversationId ="1bcs"}
            };
            baseService.GetDataAsync<AnnouncementNotification>(Arg.Any<GetAnnouncementNotificationToSendCriteria>()).Returns(fakeNotification);

            letstalkService(Arg.Any<int>()).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Returns(new PushMessageResult { Status = 0 });
            letstalkService(Arg.Any<int>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Returns(new PushMessageResult { Status = 1 });

            //Actual
            await service.ScanThenSendAsnyc();

            //Expected
            await baseService.Received(1).ExecuteNonQueryAsync(Arg.Any<UpdateAnnouncementNotificationStatusCommand>());
            await baseService.Received(2).ExecuteNonQueryAsync(Arg.Any<WriteLogMessageCommand>());
            await letstalkService(Arg.Any<int>()).Received(1).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
            await letstalkService(Arg.Any<int>()).Received(1).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
        }

        [Fact]
        public async Task ScanThenSendAsnyc_OnlyHaveRetryOnlyImage_ThrowExceptionStillUpdateStatusAndNoStuck()
        {
            //Arrange
            var fakeNotification = new List<AnnouncementNotification>
            {
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 1, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =1, ConversationId ="abc"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 1, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =2, ConversationId ="def"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 1, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =3, ConversationId ="agh"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 2, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =4, ConversationId ="asc"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 2, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =5, ConversationId ="uhj"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 2, IsOnlyRetryImage = true, ErrImages="1", ConversationGroupId =6, ConversationId ="1bcs"}
            };
            baseService.GetDataAsync<AnnouncementNotification>(Arg.Any<GetAnnouncementNotificationToSendCriteria>()).Returns(fakeNotification);

            letstalkService(Arg.Any<int>()).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Throws(new Exception());
            letstalkService(Arg.Any<int>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Throws(new Exception());

            //Actual
            await service.ScanThenSendAsnyc();

            //Expected
            await baseService.Received(1).ExecuteNonQueryAsync(Arg.Any<UpdateAnnouncementNotificationStatusCommand>());
            await baseService.Received(2).ExecuteNonQueryAsync(Arg.Any<WriteLogMessageCommand>());
            await letstalkService(Arg.Any<int>()).Received(1).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
            await letstalkService(Arg.Any<int>()).Received(1).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
        }

        [Fact]
        public async Task ScanThenSendAsnyc_WhenOnlyHaveAnnoucementNotificationToSend_SendSuccessContenAndImage_WillCallExpectedServices()
        {
            var data = FakeNotification();
            baseService.GetDataAsync<AnnouncementNotification>(Arg.Any<GetAnnouncementNotificationToSendCriteria>()).Returns(data);
            letstalkService(Arg.Any<int>()).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Returns(new PushMessageResult { Status = 1 });
            letstalkService(Arg.Any<int>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Returns(new PushMessageResult { Status = 1 });

            //Actual
            await service.ScanThenSendAsnyc();

            //Expected
            await baseService.Received(1).ExecuteNonQueryAsync(Arg.Any<UpdateAnnouncementNotificationStatusCommand>());
            await baseService.Received(8).ExecuteNonQueryAsync(Arg.Any<WriteLogMessageCommand>());

            //Received 4 times call because group announcement notification with difference announcementId & send image
            await letstalkService(Arg.Any<int>()).Received(2).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Html));
            await letstalkService(Arg.Any<int>()).Received(2).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Image));
            await letstalkService(Arg.Any<int>()).Received(2).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Html));
            await letstalkService(Arg.Any<int>()).Received(2).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Image));
        }

        [Fact]
        public async Task ScanThenSendAsnyc_WhenOnlyHaveAnnoucementNotificationToSend_SendUnSuccessConten_WillNotSendImage()
        {
            var data = FakeNotification();
            baseService.GetDataAsync<AnnouncementNotification>(Arg.Any<GetAnnouncementNotificationToSendCriteria>()).Returns(data);
            letstalkService(Arg.Any<int>()).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Returns(new PushMessageResult { Status = 0 });
            letstalkService(Arg.Any<int>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>()).Returns(new PushMessageResult { Status = 0 });

            //Actual
            await service.ScanThenSendAsnyc();

            //Expected
            await baseService.Received(1).ExecuteNonQueryAsync(Arg.Any<UpdateAnnouncementNotificationStatusCommand>());
            await baseService.Received(4).ExecuteNonQueryAsync(Arg.Any<WriteLogMessageCommand>());

            //Received 4 times call because group announcement notification with difference announcementId
            await letstalkService(Arg.Any<int>()).Received(2).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
            await letstalkService(Arg.Any<int>()).Received(2).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Any<OutgoingMessage>());
        }

        [Fact]
        public async Task ScanThenSendAsnyc_WhenOnlyHaveAnnoucementNotificationToSend_SendImageThrowsException_StillUpdateStatus()
        {
            var data = FakeNotification();
            baseService.GetDataAsync<AnnouncementNotification>(Arg.Any<GetAnnouncementNotificationToSendCriteria>()).Returns(data);
            letstalkService(Arg.Any<int>()).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Html)).Returns(new PushMessageResult { Status = 1 });
            letstalkService(Arg.Any<int>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Html)).Returns(new PushMessageResult { Status = 1 });

            letstalkService(Arg.Any<int>()).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Image)).Throws(new Exception());
            letstalkService(Arg.Any<int>()).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Image)).Throws(new Exception());
            //Actual
            await service.ScanThenSendAsnyc();

            //Expected
            await baseService.Received(1).ExecuteNonQueryAsync(Arg.Any<UpdateAnnouncementNotificationStatusCommand>());
            await baseService.Received(8).ExecuteNonQueryAsync(Arg.Any<WriteLogMessageCommand>());

            //Received 4 times call because group announcement notification with difference announcementId
            await letstalkService(Arg.Any<int>()).Received(2).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Html));
            await letstalkService(Arg.Any<int>()).Received(2).PushMultiRoomAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Image));
            await letstalkService(Arg.Any<int>()).Received(2).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Html));
            await letstalkService(Arg.Any<int>()).Received(2).PushPersonalAsync(Arg.Any<List<string>>(), Arg.Is<OutgoingMessage>(x => x.MessageType == MessageTypeNumber.Image));
        }

        private static List<AnnouncementNotification> FakeNotification()
        {
            var fakeNotification = new List<AnnouncementNotification>
            {
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 1,  Content="1", FileName ="1", ConversationGroupId =1, ConversationId ="abc"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 2, ChatTypeId = 1,  Content="1", FileName ="1", ConversationGroupId =2, ConversationId ="def"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 1,  Content="1", FileName ="1", ConversationGroupId =3, ConversationId ="agh"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 2,  Content="1", FileName ="1", ConversationGroupId =4, ConversationId ="asc"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 2, ChatTypeId = 2,  Content="1", FileName ="1", ConversationGroupId =5, ConversationId ="uhj"},
                new AnnouncementNotification {BotId = 1, AnnouncementId = 1, ChatTypeId = 2,  Content="1", FileName ="1", ConversationGroupId =6, ConversationId ="1bcs"}
            };
            return fakeNotification;
        }
    }
}