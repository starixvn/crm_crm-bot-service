using CRM.BotService.Core.Enum;
using CRM.BotService.Core.Models.Base;

namespace CRM.BotService.Endpoint.Controllers
{
    using Core.Constants;
    using Core.Constants.Notification;
    using Core.Criteria;
    using Core.Models.Letstalk;
    using Core.Models.Notification;
    using Core.Services;
    using Core.Services.Interfaces;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using ViewModel;

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class LetstalkController : Controller
    {
        private readonly Func<long, ILetstalkClientService> letstalkClientService;
        private readonly ILetstalkSendMultipleReceiverService letstalkSendMultipleReceiverService;
        private readonly IBotCommonService botCommonService;
        private readonly ILogServiceExtend logService;

        public LetstalkController(Func<long, ILetstalkClientService> letstalkClientService,
            IBotCommonService botCommonService,
            ILogServiceExtend logService,
            ILetstalkSendMultipleReceiverService letstalkSendMultipleReceiverService)
        {
            this.letstalkClientService = letstalkClientService;
            this.logService = logService;
            this.botCommonService = botCommonService;
            this.letstalkSendMultipleReceiverService = letstalkSendMultipleReceiverService;
        }

        [HttpPost]
        [Route("api/letstalk/get-user-profile")]
        public async Task<IActionResult> CheckPersonalProfileAsync([FromBody] LetstalkBotConversationViewModel model)
        {
            var profile = await letstalkClientService(model.BotId).GetProfileAsync(model.LetstalkId);
            return Ok(profile);
        }

        [HttpPost]
        [Route("api/letstalk/verify-on-group")]
        public async Task<IActionResult> VerifyUserOnGroupAsync([FromBody] VerifyUserOnGroupViewModel model)
        {
            var results = new List<object>();
            var roomList = await letstalkClientService(model.BotId).GetChatRoomList();
            bool isBotRemoved = !roomList.Any(room => room.Equals(model.ConversationGroupId, StringComparison.CurrentCultureIgnoreCase));
            if (!isBotRemoved)
            {
                var botFriends = await letstalkClientService(model.BotId).GetBotContactList();
                var roomData = await letstalkClientService(model.BotId).GetChatRoomData(model.ConversationGroupId);

                foreach (var letstalkId in model.BotAccountIds)
                {
                    results.Add(new
                    {
                        LetstalkId = letstalkId,
                        IsGroupMember = roomData.Members.Contains(letstalkId),
                        IsBotFriend = botFriends.Contains(letstalkId)
                    });
                }
            }

            return Ok(new
            {
                IsBotRemoved = isBotRemoved,
                Letstalks = results
            });
        }

        [HttpPost]
        [Route("api/letstalk/send-plaintext-multiple")]
        public async Task<IActionResult> SendPlainTextMultipleMessage([FromBody] IEnumerable<NotificationMessage> notifications)
        {
            var tasks = new List<Task<SendResult>>();

            if (notifications is not null && notifications.Any())
            {
                foreach (var notification in notifications)
                {
                    tasks.Add(
                        Task.Run(
                            () => SendLetstalkService(notification)
                            ));
                }
            }

            var results = (await Task.WhenAll(tasks)).ToList();

            return Ok(results);
        }

        [HttpPost]
        [Route("api/letstalk/send-multiple-announcement")]
        public async Task<IActionResult> SendMultipleFormattedTextAndImagesAsync([FromBody] IEnumerable<AnnouncementNotification> notifications)
        {
            logService.LogInformation("==================Send Announcement Started=================");
            logService.LogInformation(JsonConvert.SerializeObject(notifications));
            var results = await letstalkSendMultipleReceiverService.SendFornattedTextAndImagesAsync(notifications);
            logService.LogInformation(JsonConvert.SerializeObject(results));

            return Ok(results);
        }

        [HttpPost]
        [Route("api/letstalk/send-image-only")]
        public async Task<IActionResult> SendOnlyImagesAync([FromBody] IEnumerable<AnnouncementNotification> notifications)
        {
            logService.LogInformation("==================Resend images started=================");
            logService.LogInformation(JsonConvert.SerializeObject(notifications));

            var results = await letstalkSendMultipleReceiverService.SendImageAsync(notifications);

            return Ok(results);
        }

        [HttpPost]
        [Route("api/letstalk/send-plaintext-single")]
        public async Task<IActionResult> SendSingleMessage([FromBody] NotificationMessage notification)
        {
            var result = await SendLetstalkService(notification);
            logService.LogInformation("Single message sent result: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        private async Task<SendResult> SendLetstalkService(NotificationMessage notification)
        {
            notification.Content = RemoveUnsupportedTags(notification.Content);

            var result = new SendResult
            {
                NotificationId = notification.NotificationId,
                ConversationGroupId = notification.ConversationGroupId,
            };

            if (!botCommonService.IsActiveChannel(notification.ChannelId))
            {
                result.SentTime = DateTimeOffset.UtcNow.DateTime;
                result.ErrorCode = 1;
                result.ErrorMessage = $"Channel {notification.ChannelId} is current inactivated";
                result.IsIgnoreRetry = true;
                return result;
            }

            try
            {
                if (notification.ChatType == ChatType.Personal)
                {
                    await SendLetstalkPersonal(notification, result);
                }
                else
                {
                    await SendLetstalkGroup(notification, result);
                }
            }
            catch (Exception ex)
            {
                var writeLogCommand = new WriteLogMessageCommand
                {
                    ReferenceId = notification.NotificationId,
                    BotId = notification.BotId,
                    ConversationId = notification.ConversationId,
                    Content = notification.Content,
                    IsSuccess = false,
                    IsBotBlocked = false,
                    ErrMsg = ex.Message
                };
                await botCommonService.WriteLogMessageAsync(writeLogCommand);

                result.SentTime = DateTimeOffset.UtcNow.DateTime;
                result.ErrorCode = 1;
                result.ErrorMessage = ex.Message;
                await logService.ErrorMessageAsync(JsonConvert.SerializeObject(notification));
                await logService.ErrorAsync(ex);
            }

            return result;
        }

        private static string RemoveUnsupportedTags(string content)
        {
            string[] allowedTagsHtml = { "b", "strong", "i", "em", "ins", "u", "s", "strike", "del", "a", "tg-emoji", "code", "pre", "br" };
            var regex = new Regex(@"(<br />|<hr />)");
            var replaceNewLine = regex.Replace(content, Environment.NewLine);
            string messageContentReplace = Regex.Replace(replaceNewLine, "<(?!/?(" + string.Join("|", allowedTagsHtml) + ")(>|\\s.*?>)).*?>", "");
            return messageContentReplace;
        }

        private async Task SendLetstalkPersonal(NotificationMessage notification, SendResult result)
        {
            var outGoingMsg = new OutgoingMessage
            {
                MessageType = NotificationMessageType.PlainText,
                Text = notification.Content
            };

            var sendResult = await letstalkClientService(notification.BotId).PushPersonalAsync(notification.ConversationId, outGoingMsg);
            logService.LogInformation("SendLetstalkPersonal: outout=" + JsonConvert.SerializeObject(sendResult) + "   input=" + JsonConvert.SerializeObject(notification));

            result.SentTime = DateTimeOffset.UtcNow.DateTime;
            result.ErrorCode = sendResult.IsSuccess && !sendResult.BlockList.Any() ? 0 : 1;
            result.IsBlockedBot = sendResult.BlockList.Exists(x => x.Equals(notification.ConversationId, StringComparison.CurrentCultureIgnoreCase));
            result.ErrorMessage = !sendResult.IsSuccess &&
                sendResult.FailList.Exists(x => x.Equals(notification.ConversationId, StringComparison.CurrentCultureIgnoreCase))
                ? SendNotificationErrorMessage.LetstalkIdInvalid
                : string.Empty;

            var writeLogCommand = new WriteLogMessageCommand
            {
                ReferenceId = notification.NotificationId,
                DataSource = LogDataSource.Notification,
                BotId = notification.BotId,
                ConversationId = notification.ConversationId,
                Content = notification.Content,
                IsSuccess = sendResult.IsSuccess,
                IsBotBlocked = sendResult.BlockList.Any(),
                ErrMsg = sendResult.Message
            };
            await botCommonService.WriteLogMessageAsync(writeLogCommand);
        }

        private async Task SendLetstalkGroup(NotificationMessage notification, SendResult result)
        {
            var outGoingMsg = new OutgoingMessage
            {
                MessageType = NotificationMessageType.PlainText,
                Text = notification.Content
            };

            var sendResult = await letstalkClientService(notification.BotId).PushRoomAsync(notification.ConversationId, outGoingMsg);

            result.SentTime = DateTimeOffset.UtcNow.DateTime;
            result.ErrorCode = sendResult.IsSuccess ? 0 : 1;
            result.ErrorMessage = !sendResult.IsSuccess ? string.Format(SendNotificationErrorMessage.BotServiceErrorMsg,
                BotChannel.Letstalk.ToString().ToLowerInvariant()) : string.Empty;

            var writeLogCommand = new WriteLogMessageCommand
            {
                ReferenceId = notification.NotificationId,
                BotId = notification.BotId,
                ConversationId = notification.ConversationId,
                Content = notification.Content,
                IsSuccess = sendResult.IsSuccess,
                IsBotBlocked = sendResult.BlockList.Any(),
                ErrMsg = sendResult.Message
            };
            await botCommonService.WriteLogMessageAsync(writeLogCommand);
        }
    }
}