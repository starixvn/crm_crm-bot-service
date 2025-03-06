namespace CRM.BotService.Endpoint.Controllers;

using Constants;
using Criteria;
using Enum;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Base;
using Models.Letstalk;
using Models.Notification;
using Services;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ViewModel;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class LetstalkController(Func<long, ILetstalkClientService> letstalkClientService,
    IBotCommonService botCommonService,
    ILogServiceExtend logService,
    ILetstalkSendMultipleReceiverService letstalkSendMultipleReceiverService) : Controller
{
    private const string LetstalkBotServiceErrorMessage = "Error occurred on letstalk chat bot service";
    private const string InvalidLetstalkId = "The Letstalk ID has been changed";

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
        var isBotRemoved = !roomList.Any(room => room.Equals(model.ConversationGroupId, StringComparison.CurrentCultureIgnoreCase));
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
    public async Task<IActionResult> SendPlainTextMultipleMessage([FromBody] List<NotificationMessage> notifications)
    {
        var tasks = new List<Task<SendResult>>();

        if (notifications is not null && notifications.Count > 0)
        {
            tasks.AddRange(notifications.Select(notification => Task.Run(() => SendLetstalkService(notification))));
        }

        var results = (await Task.WhenAll(tasks)).ToList();

        return Ok(results);
    }

    [HttpPost]
    [Route("api/letstalk/send-multiple-announcement")]
    public async Task<IActionResult> SendMultipleFormattedTextAndImagesAsync([FromBody] List<AnnouncementNotification> notifications)
    {
        var results = await letstalkSendMultipleReceiverService.SendFormattedTextAndImagesAsync(notifications);

        return Ok(results);
    }

    [HttpPost]
    [Route("api/letstalk/send-image-only")]
    public async Task<IActionResult> SendOnlyImagesAsync([FromBody] List<AnnouncementNotification> notifications)
    {
        var results = await letstalkSendMultipleReceiverService.SendImageAsync(notifications);

        return Ok(results);
    }

    [HttpPost]
    [Route("api/letstalk/send-plaintext-single")]
    public async Task<IActionResult> SendSingleMessage([FromBody] NotificationMessage notification)
    {
        var result = await SendLetstalkService(notification);
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
            await logService.ErrorAsync(ex);
        }

        return result;
    }

    private static string RemoveUnsupportedTags(string content)
    {
        string[] allowedTagsHtml = ["b", "strong", "i", "em", "ins", "u", "s", "strike", "del", "a", "tg-emoji", "code", "pre", "br"
        ];
        var regex = new Regex("(<br />|<hr />)");
        var replaceNewLine = regex.Replace(content, Environment.NewLine);
        var messageContentReplace = Regex.Replace(replaceNewLine, "<(?!/?(" + string.Join("|", allowedTagsHtml) + ")(>|\\s.*?>)).*?>", "");
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

        result.SentTime = DateTimeOffset.UtcNow.DateTime;
        result.ErrorCode = sendResult.IsSuccess && !sendResult.BlockList.Any() ? 0 : 1;
        result.IsBlockedBot = sendResult.BlockList.Exists(x => x.Equals(notification.ConversationId, StringComparison.CurrentCultureIgnoreCase));
        result.ErrorMessage = !sendResult.IsSuccess &&
                              sendResult.FailList.Exists(x => x.Equals(notification.ConversationId, StringComparison.CurrentCultureIgnoreCase))
            ? InvalidLetstalkId
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
        result.ErrorMessage = !sendResult.IsSuccess ? LetstalkBotServiceErrorMessage : string.Empty;

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