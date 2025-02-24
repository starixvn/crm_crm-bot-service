using CRM.BotService.Criteria;
using CRM.BotService.Enum;
using CRM.BotService.Models.Letstalk.Result;
using CRM.BotService.Models.Notification;
using CRM.BotService.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.BotService.Services;

public interface ILetstalkSendMultipleReceiverService
{
    Task<IEnumerable<SendNotificationResult>> SendFormattedTextAndImagesAsync(List<AnnouncementNotification> notifications);

    Task<IEnumerable<SendNotificationResult>> SendImageAsync(List<AnnouncementNotification> notifications);
}

public class LetstalkSendMultipleReceiverService(
    IBotCommonService botCommonService,
    ILogServiceExtend logService,
    Func<long, ILetstalkClientService> letstalkService)
    : ILetstalkSendMultipleReceiverService
{
    private const string LetstalkBotServiceErrorMessage = "Error occurred on letstalk chat bot service";

    private const byte MaxReceiversOnOneBatch = 10;

    public async Task<IEnumerable<SendNotificationResult>> SendFormattedTextAndImagesAsync(List<AnnouncementNotification> notifications)
    {
        var sendResults = new List<SendNotificationResult>();
        if (notifications.Count > 0)
        {
            var groupedContentNotifications = GroupNotificationByContentAndChatType(notifications);
            var tasks = groupedContentNotifications.Select(notification => Task.Run(() => SendToLetstalkAsync(notification))).ToList();

            var parallelResult = (await Task.WhenAll(tasks)).ToList();

            foreach (var result in parallelResult)
            {
                sendResults.AddRange(result);
            }
        }

        return sendResults;
    }

    public async Task<IEnumerable<SendNotificationResult>> SendImageAsync(List<AnnouncementNotification> notifications)
    {
        List<SendNotificationResult> results = [];

        if (notifications.Count > 0)
        {
            var groupedNotifications = GroupResendErrorImages(notifications);
            var tasks = groupedNotifications.Select(notification => Task.Run(() => ResendErrorImagesAsync(notification))).ToList();

            var sendResults = (await Task.WhenAll(tasks)).ToList();

            results = MergedResultList(sendResults);
        }
        return results;
    }

    private async Task<List<SendNotificationResult>> ResendErrorImagesAsync(NotificationGroupContent notification)
    {
        var sendResult = notification.ConversationIds.Select(conversationId =>
            new SendNotificationResult
            {
                AnnouncementId = notification.AnnouncementId,
                ConversationId = conversationId,
                IsSuccessful = true,
                ErrMsg = string.Empty
            }).ToList();

        if (notification.ChatType == ChatType.Personal)
        {
            await SendImageMultiplePersonalAsync(notification.AnnouncementId, notification.ConversationIds, notification.BotId, notification.Attachments, sendResult);
        }
        else
        {
            await SendImageToMultipleGroupAsync(notification.AnnouncementId, notification.ConversationIds, notification.BotId, notification.Attachments, sendResult);
        }

        return sendResult;
    }

    private static List<SendNotificationResult> MergedResultList(List<List<SendNotificationResult>> sendReults)
    {
        var results = new List<SendNotificationResult>();
        foreach (var sendResult in sendReults)
        {
            results.AddRange(sendResult);
        }
        return results;
    }

    private List<NotificationGroupContent> GroupResendErrorImages(IEnumerable<AnnouncementNotification> notifications)
    {
        var groupedNotification = notifications.GroupBy(i =>
            new
            {
                i.AnnouncementId,
                i.BotId,
                i.ChatTypeId,
                i.ErrImages
            }).Select(item =>
        {
            return new NotificationGroupContent
            {
                AnnouncementId = item.Key.AnnouncementId,
                BotId = item.Key.BotId,
                ChatType = item.Key.ChatTypeId,
                ConversationIds = item.Select(x => x.ConversationId).ToList(),
                Attachments = item.FirstOrDefault()?.RetryImages.ToList(),
            };
        });
        var results = DividedBatchRecivers(groupedNotification);
        return results;
    }

    private async Task<List<SendNotificationResult>> SendToLetstalkAsync(NotificationGroupContent notification)
    {
        var sendResult = notification.ChatType == ChatType.Personal
            ? await SendMessageToMultiplePersonalAsync(notification)
            : await SendMessageToMultipleGroupAsync(notification);
        return sendResult;
    }

#pragma warning disable S3776

    private async Task<List<SendNotificationResult>> SendMessageToMultiplePersonalAsync(NotificationGroupContent notification)
#pragma warning restore S3776
    {
        var sendResult = notification.ConversationIds.Select(conversationId =>
            new SendNotificationResult
            {
                AnnouncementId = notification.AnnouncementId,
                ConversationId = conversationId,
                IsSuccessful = true,
                ErrMsg = string.Empty
            }).ToList();

        if (!string.IsNullOrWhiteSpace(notification.Content))
        {
            try
            {
                var pushResults = await letstalkService(notification.BotId).PushPersonalAsync(notification.ConversationIds,
                    new()
                    {
                        MessageType = NotificationMessageType.Html,
                        Text = notification.Content
                    });
                WriteLogMessage(notification.AnnouncementId, notification.BotId, notification.Content,
                    string.Join(",", notification.ConversationIds), pushResults.IsSuccess, JsonConvert.SerializeObject(pushResults));

                if (pushResults.IsSuccess)
                {
#pragma warning disable S134
                    if (pushResults.FailList is not null && pushResults.FailList.Count > 0)
#pragma warning restore S134
                    {
                        HandleFailedList(pushResults.FailList, sendResult);
                    }

#pragma warning disable S134
                    if (pushResults.BlockList is not null && pushResults.BlockList.Count > 0)
#pragma warning restore S134
                    {
                        HandlleBlockList(pushResults.BlockList, sendResult);
                    }
                }
                else
                {
                    SetFailedResultWithErrMsg(sendResult);
                }
            }
            catch (Exception exception)
            {
                SetFailedResultWithErrMsg(sendResult);
                WriteLogMessage(notification.AnnouncementId, notification.BotId, notification.Content,
                    string.Join(",", notification.ConversationIds), false, exception.Message);
                await logService.ErrorAsync(exception);
            }
        }

        var successList = sendResult.Where(i => i.IsSuccessful).Select(x => x.ConversationId).ToList();
        if (successList.Any() && notification.Attachments is not null && notification.Attachments.Any())
        {
            await SendImageMultiplePersonalAsync(notification.AnnouncementId, successList, notification.BotId, notification.Attachments, sendResult);
        }

        return sendResult;
    }

    private async Task SendImageMultiplePersonalAsync(int announcementId, List<string> conversationIds, long botId, List<string> images, List<SendNotificationResult> sendResult)
    {
        var pushResult = new PushMessageResult();
        foreach (var img in images)
        {
            try
            {
                pushResult = await letstalkService(botId).PushPersonalAsync(conversationIds, new()
                {
                    MessageType = NotificationMessageType.Image,
                    ImageUrl = img
                });
                WriteLogMessage(announcementId, botId, img, string.Join(",", conversationIds), pushResult.IsSuccess, JsonConvert.SerializeObject(pushResult));
            }
            catch (Exception ex)
            {
                pushResult.Status = 0;
                pushResult.Message = ex.Message;
                pushResult.FailList = conversationIds;
                WriteLogMessage(announcementId, botId, img, string.Join(",", conversationIds), pushResult.IsSuccess, JsonConvert.SerializeObject(pushResult));
                await logService.ErrorAsync(ex);
            }

            UpdateSendImageResultStatus(pushResult, sendResult, img);
        }
    }

    private static void UpdateSendImageResultStatus(PushMessageResult pushResult, List<SendNotificationResult> sendResult, string image)
    {
        if (!pushResult.IsSuccess)
        {
            foreach (var result in sendResult)
            {
                SetResultSendImage(result, image);
            }
        }
        else
        {
            if (pushResult.FailList.Any())
            {
                foreach (var result in sendResult.Where(result => pushResult.FailList.Exists(i => i.Equals(result.ConversationId))))
                {
                    SetResultSendImage(result, image);
                }
            }

            if (pushResult.BlockList.Any())
            {
                HandlleBlockList(pushResult.BlockList, sendResult);
            }
        }
    }

    private static void SetResultSendImage(SendNotificationResult result, string failedImage)
    {
        result.IsSuccessful = false;
        result.IsOnlyRetryImage = true;
        result.ErrMsg = LetstalkBotServiceErrorMessage;
        result.ErrImages.Add(failedImage);
    }

    private static void HandleFailedList(List<string> failList, List<SendNotificationResult> sendResult)
    {
        foreach (var result in sendResult.Where(result => failList.Exists(i => i.Equals(result.ConversationId))))
        {
            result.IsSuccessful = false;
            result.ErrMsg = LetstalkBotServiceErrorMessage;
        }
    }

    private static void HandlleBlockList(List<string> blockList, List<SendNotificationResult> sendResult)
    {
        foreach (var result in sendResult.Where(result => blockList.Exists(i => i.Equals(result.ConversationId))))
        {
            result.IsSuccessful = false;
            result.IsBotBlocked = true;
        }
    }

    private static void SetFailedResultWithErrMsg(List<SendNotificationResult> results)
    {
        foreach (var item in results)
        {
            item.IsSuccessful = false;
            item.ErrMsg = LetstalkBotServiceErrorMessage;
        }
    }

    private List<NotificationGroupContent> GroupNotificationByContentAndChatType(IEnumerable<AnnouncementNotification> notifications)
    {
        logService.LogInformation("========== start group content===========");
        logService.LogInformation(JsonConvert.SerializeObject(notifications));

        var groupedNotification = notifications.GroupBy(i =>
            new
            {
                i.AnnouncementId,
                i.Content,
                i.BotId,
                i.ChatTypeId,
                _ = (i.Images != null && i.Images.Any()) ? string.Join(",", i.Images) : null
            }).Select(item =>
        {
            return new NotificationGroupContent
            {
                AnnouncementId = item.Key.AnnouncementId,
                BotId = item.Key.BotId,
                Content = item.Key.Content,
                ChatType = item.Key.ChatTypeId,
                ConversationIds = item.Select(x => x.ConversationId).ToList(),
                Attachments = item.FirstOrDefault()?.Images?.ToList(),
            };
        });
        logService.LogInformation("========== end group content===========");

        var results = DividedBatchRecivers(groupedNotification);
        return results;
    }

    private List<NotificationGroupContent> DividedBatchRecivers(IEnumerable<NotificationGroupContent> notifications)
    {
        logService.LogInformation("========== start divide batch group content===========");
        logService.LogInformation(JsonConvert.SerializeObject(notifications));
        var listResults = new List<NotificationGroupContent>();
        foreach (var item in notifications)
        {
            if (item.ConversationIds.Count <= MaxReceiversOnOneBatch)
            {
                listResults.Add(item);
            }
            else
            {
                var batches = DivideBatchConversationIds(item);
                listResults.AddRange(batches);
            }
        }
        logService.LogInformation("========== end divide batch group content===========");

        return listResults;
    }

    private static List<NotificationGroupContent> DivideBatchConversationIds(NotificationGroupContent item)
    {
        var totalItem = item.ConversationIds.Count;
        var listResults = new List<NotificationGroupContent>();
        var totalBatch = (int)Math.Ceiling((double)totalItem / MaxReceiversOnOneBatch);
        for (var i = 0; i < totalBatch; i++)
        {
            listResults.Add(new()
            {
                AnnouncementId = item.AnnouncementId,
                BotId = item.BotId,
                Content = item.Content,
                ChatType = item.ChatType,
                ConversationIds = item.ConversationIds.Skip(i * MaxReceiversOnOneBatch).Take(MaxReceiversOnOneBatch).ToList(),
                Attachments = item.Attachments
            });
        }
        return listResults;
    }

    private void WriteLogMessage(int announcementId, long botId, string content, string conversationIds, bool isSuccess, string errMsg)
    {
        var command = new WriteLogMessageCommand
        {
            ReferenceId = announcementId,
            DataSource = "DBCRM.bodb_CRM.dbo.crmAnnouncement",
            BotId = botId,
            Content = content,
            ConversationId = conversationIds,
            IsSuccess = isSuccess,
            ErrMsg = errMsg,
        };
        Task.Run(async () => await botCommonService.WriteLogMessageAsync(command))
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    logService.ErrorAsync(task.Exception);
                }
            });
    }

    private async Task<List<SendNotificationResult>> SendMessageToMultipleGroupAsync(NotificationGroupContent notification)
    {
        var sendResult = notification.ConversationIds.Select(conversationId =>
            new SendNotificationResult
            {
                AnnouncementId = notification.AnnouncementId,
                ConversationId = conversationId,
                IsSuccessful = true,
                ErrMsg = string.Empty
            }).ToList();

        if (!string.IsNullOrWhiteSpace(notification.Content))
        {
            try
            {
                var pushResults = await letstalkService(notification.BotId).PushRoomAsync(notification.ConversationIds,
                    new()
                    {
                        MessageType = NotificationMessageType.Html,
                        Text = notification.Content
                    });
                WriteLogMessage(notification.AnnouncementId, notification.BotId, notification.Content,
                    string.Join(",", notification.ConversationIds), pushResults.IsSuccess, JsonConvert.SerializeObject(pushResults));
                if (pushResults.IsSuccess)
                {
#pragma warning disable S134
                    if (pushResults.FailList is not null && pushResults.FailList.Count > 0)
#pragma warning restore S134
                    {
                        HandleFailedList(pushResults.FailList, sendResult);
                    }
                }
                else
                {
                    SetFailedResultWithErrMsg(sendResult);
                }
            }
            catch (Exception exception)
            {
                SetFailedResultWithErrMsg(sendResult);
                WriteLogMessage(notification.AnnouncementId, notification.BotId, notification.Content,
                    string.Join(",", notification.ConversationIds), false, exception.Message);
                await logService.ErrorAsync(exception);
            }
        }

        var successList = sendResult.Where(i => i.IsSuccessful).Select(x => x.ConversationId).ToList();
        if (successList.Any() && notification.Attachments is not null && notification.Attachments.Any())
        {
            await SendImageToMultipleGroupAsync(notification.AnnouncementId, successList, notification.BotId, notification.Attachments, sendResult);
        }

        return sendResult;
    }

    private async Task SendImageToMultipleGroupAsync(int announcementId, List<string> conversationIds, long botId, List<string> images, List<SendNotificationResult> sendResult)
    {
        var pushResult = new PushMessageResult();
        foreach (var img in images)
        {
            try
            {
                pushResult = await letstalkService(botId).PushRoomAsync(conversationIds, new()
                {
                    MessageType = NotificationMessageType.Image,
                    ImageUrl = img
                });
                WriteLogMessage(announcementId, botId, img, string.Join(",", conversationIds), pushResult.IsSuccess, JsonConvert.SerializeObject(pushResult));
            }
            catch (Exception ex)
            {
                pushResult.Status = 0;
                pushResult.Message = ex.Message;
                pushResult.FailList = conversationIds;
                WriteLogMessage(announcementId, botId, img, string.Join(",", conversationIds), pushResult.IsSuccess, JsonConvert.SerializeObject(pushResult));
            }
            UpdateSendImageResultStatus(pushResult, sendResult, img);
        }
    }
}