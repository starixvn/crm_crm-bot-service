using CRM.BotService.Core.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CRM.BotService.Endpoint.Controllers.Telegram
{
    using Core.Constants;
    using Core.Constants.Notification;
    using Core.Criteria;
    using Core.Enum;
    using Core.Models.Base;
    using Core.Models.Notification;
    using Core.Services.Interfaces;
    using Fanex.Caching;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ViewModel;

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MessageController : Controller
    {
        private readonly ILogServiceExtend logService;
        private readonly Func<long, ITelegramBotService> telegramBotService;
        private readonly IBotCommonService botCommonService;
        private readonly ICacheService cacheService;
        private readonly string BaseImageUrl;
        private readonly string BaseImageServiceNameUrl;
        private static readonly string TelegramBotServiceErrorMessage = string.Format(SendNotificationErrorMessage.BotServiceErrorMsg, BotChannel.Telegram.ToString().ToLowerInvariant());

        public MessageController(
            ILogServiceExtend logService,
            Func<long, ITelegramBotService> telegramBotService,
            IBotCommonService botCommonService,
            IConfiguration configuration,
            ICacheService cacheService)
        {
            this.logService = logService;
            this.telegramBotService = telegramBotService;
            this.botCommonService = botCommonService;
            this.cacheService = cacheService;
            BaseImageUrl = configuration.GetValue<string>("BaseImageUrl");
            BaseImageServiceNameUrl = configuration.GetValue<string>("BaseImageServiceNameUrl");
        }

        [HttpPost]
        [Route("api/telegram/send-plaintext-multiple")]
        public async Task<IActionResult> SendMultipleMessage([FromBody] IEnumerable<NotificationMessage> notifications)
        {
            logService.LogInformation("Start SendMultipleMessage: " + JsonConvert.SerializeObject(notifications, Formatting.Indented));
            var tasks = new List<Task<SendResult>>();
            if (notifications is not null && notifications.Any())
            {
                foreach (var notification in notifications)
                {
                    tasks.Add(
                        Task.Run(
                            () => SendTelegramService(notification)
                    ));
                }
            }

            var results = (await Task.WhenAll(tasks)).ToList();
            logService.LogInformation("End SendMultipleMessage: " + JsonConvert.SerializeObject(results, Formatting.Indented));
            return Ok(results);
        }

        [HttpPost]
        [Route("api/telegram/send-multiple-announcement")]
        public async Task<IActionResult> SendMultipleFormattedTextAndImagesAsync([FromBody] IEnumerable<AnnouncementNotification> notifications)
        {
            logService.LogInformation("Start SendMultipleFormattedTextAndImagesAsync: " + JsonConvert.SerializeObject(notifications, Formatting.Indented));

            if (!botCommonService.IsActiveChannel(BotChannel.Telegram))
            {
                var inActivedBotChannelResults = notifications.Select(x => new SendNotificationResult
                {
                    AnnouncementId = x.AnnouncementId,
                    ConversationId = x.ConversationId,
                    IsSuccessful = false,
                    IgnoredRetry = true,
                    ErrMsg = $"Channel {x.ChannelId} is current inactivated"
                });
                logService.LogInformation($"{BotChannel.Telegram} current inactived");

                return Ok(inActivedBotChannelResults);
            }
            var tasks = new List<Task<SendNotificationResult>>();
            if (notifications is not null && notifications.Any())
            {
                foreach (var notification in notifications)
                {
                    tasks.Add(
                        Task.Run(
                            () => SendFormattedTextAndImageTelegramService(notification)
                    ));
                }
            }
            var results = (await Task.WhenAll(tasks)).ToList();
            logService.LogInformation("End SendMultipleFormattedTextAndImagesAsync: " + JsonConvert.SerializeObject(results, Formatting.Indented));
            return Ok(results);
        }

        [HttpPost]
        [Route("api/telegram/send-image-only")]
        public async Task<IActionResult> SendMultipleImagesOnlyAsync([FromBody] IEnumerable<AnnouncementNotification> notifications)
        {
            logService.LogInformation("Start SendMultipleImagesOnlyAsync: " + JsonConvert.SerializeObject(notifications, Formatting.Indented));
            if (!botCommonService.IsActiveChannel(BotChannel.Telegram))
            {
                var inActivedBotChannelResults = notifications.Select(x => new SendNotificationResult
                {
                    AnnouncementId = x.AnnouncementId,
                    ConversationId = x.ConversationId,
                    IsSuccessful = false,
                    IgnoredRetry = true,
                    ErrMsg = $"Channel {x.ChannelId} is current inactivated"
                });
                logService.LogInformation($"{BotChannel.Telegram} current inactived");
                return Ok(inActivedBotChannelResults);
            }

            var tasks = new List<Task<SendNotificationResult>>();
            if (notifications is not null && notifications.Any())
            {
                foreach (var notification in notifications)
                {
                    tasks.Add(
                        Task.Run(
                            () => SendImagesOnlyTelegramService(notification)
                    ));
                }
            }

            var results = (await Task.WhenAll(tasks)).ToList();

            logService.LogInformation("End SendMultipleImagesOnlyAsync: " + JsonConvert.SerializeObject(results, Formatting.Indented));
            return Ok(results);
        }

        [HttpPost]
        [Route("api/telegram/send-plaintext-single")]
        public async Task<IActionResult> SendSingleMessage(NotificationMessage notification)
        {
            var result = await SendTelegramService(notification);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/telegram/bot/get-user-profile")]
        public async Task<IActionResult> CheckPersonalProfileAsync([FromBody] TelegramBotConversationViewModel model)
        {
            try
            {
                var profile = await telegramBotService(model.BotId).GetChatAsync(model.BotId, model.ChatId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                await logService.ErrorAsync(ex);
                return Ok();
            }
        }

        [HttpPost]
        [Route("api/telegram/bot/verify-on-group")]
        public async Task<IActionResult> VerifyUserOnGroupAsync([FromBody] VerifyUserOnGroupViewModel model)
        {
            try
            {
                var results = new List<object>();
                _ = long.TryParse(model.ConversationGroupId, out long chatGroupId);

                var chatGroup = await telegramBotService(model.BotId).GetChatAsync(model.BotId, chatGroupId);
                var isBotRemoved = chatGroup is null;

                if (!isBotRemoved && model.BotAccountIds is not null && model.BotAccountIds.Any())
                {
                    foreach (var botAccountId in model.BotAccountIds)
                    {
                        _ = long.TryParse(botAccountId, out long chatMemberId);

                        var chatMember = await telegramBotService(model.BotId).GetChatMemberAsync(model.BotId, chatGroupId, chatMemberId);
                        results.Add(new
                        {
                            ChatId = botAccountId,
                            IsGroupMember = chatMember is not null && chatMember.Status != ChatMemberStatus.Left && chatMember.Status != ChatMemberStatus.Kicked,
                        });
                    }
                }

                return Ok(new
                {
                    IsBotRemoved = isBotRemoved,
                    Telegrams = results
                });
            }
            catch (Exception exception)
            {
                await logService.ErrorAsync(exception);
                return HandleExceptionVerifyGroup(exception);
            }
        }

        private async Task<SendResult> SendTelegramService(NotificationMessage notification)
        {
            logService.LogInformation("Start SendTelegramService!!!");
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
                logService.LogInformation(result.ErrorMessage);
                return result;
            }

            try
            {
                logService.LogInformation(string.Format("Start SendNotifyAysnc: botId: {0} chatId: {1}.", notification.BotId, notification.ConversationId));
                await telegramBotService(notification.BotId).SendNotifyAsync(notification, result);

                logService.LogInformation("end SendNotifyAysnc!!!");
            }
            catch (Exception ex)
            {
                result.ErrorCode = 1;
                result.SentTime = DateTimeOffset.UtcNow.DateTime;
                result.ErrorMessage = TelegramBotServiceErrorMessage;
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
                await logService.ErrorAsync(ex);
            }

            return result;
        }

        private async Task<SendNotificationResult> SendFormattedTextAndImageTelegramService(AnnouncementNotification announcementNotification)
        {
            var result = new SendNotificationResult
            {
                AnnouncementId = announcementNotification.AnnouncementId,
                ConversationId = announcementNotification.ConversationId
            };

            if (!string.IsNullOrWhiteSpace(announcementNotification.Content))
            {
                result = await SendFormattedTextAsync(announcementNotification);
            }

            await SendImagesIfAny(announcementNotification, result);
            return result;
        }

        private async Task SendImagesIfAny(AnnouncementNotification announcementNotification, SendNotificationResult result)
        {
            if (announcementNotification.Images?.Count > 0)
            {
                foreach (var image in announcementNotification.Images)
                {
                    await SendImageTelegramService(announcementNotification, image, result);
                }
            }
        }

        private async Task<SendNotificationResult> SendFormattedTextAsync(AnnouncementNotification announcementNotification)
        {
            logService.LogInformation(string.Format("Start SendFormattedTextAsync: botId: {0} chatId: {1}.", announcementNotification.BotId, announcementNotification.ConversationId));
            var result = new SendNotificationResult
            {
                AnnouncementId = announcementNotification.AnnouncementId,
                ConversationId = announcementNotification.ConversationId,
            };

            try
            {
                _ = long.TryParse(announcementNotification.ConversationId, out long chatId);

                var messageResult = await telegramBotService(announcementNotification.BotId).SendTextMessageAsync(announcementNotification.BotId, chatId, announcementNotification.Content);
                if (messageResult is not null)
                {
                    result.IsSuccessful = true;
                    await WriteLogAnnouncementMessageAsync(announcementNotification, announcementNotification.Content, isBotBlocked: false, result.IsSuccessful, string.Empty);
                }
            }
            catch (Exception ex)
            {
                HandleExceptionTelegramMessage(result, ex);
                await WriteLogAnnouncementMessageAsync(announcementNotification, announcementNotification.Content, result.IsBotBlocked, false, ex.Message);
                await logService.ErrorAsync(ex);
            }
            logService.LogInformation("end SendFormattedTextAsync!!!");
            return result;
        }

        private async Task SendImageTelegramService(AnnouncementNotification announcementNotification, string image, SendNotificationResult result)
        {
            logService.LogInformation("Start SendImageTelegramService!!!");

            try
            {
                logService.LogInformation(string.Format("Start SendNotifyAysnc: botId: {0} chatId: {1}.", announcementNotification.BotId, announcementNotification.ConversationId));

                _ = long.TryParse(announcementNotification.ConversationId, out long chatId);

                var message = await SendImageAsync(announcementNotification.BotId, chatId, image);

                if (message is not null)
                {
                    result.IsSuccessful = true;
                    await WriteLogAnnouncementMessageAsync(announcementNotification, image, isBotBlocked: false, result.IsSuccessful, string.Empty);
                }
            }
            catch (Exception ex)
            {
                HandleExceptionTelegramMessage(result, ex);
                result.IsOnlyRetryImage = true;
                result.ErrImages.Add(image);
                await WriteLogAnnouncementMessageAsync(announcementNotification, image, result.IsBotBlocked, false, ex.Message);
                await logService.ErrorAsync(ex);
            }

            logService.LogInformation("end SendNotifyAysnc!!!");
        }

        private async Task<Message> SendImageAsync(long botId, long chatId, string image)
        {
            string fileId = await cacheService.GetAsync<string>(GetKeyCacheFileId(image, botId));
            Message result;
            if (string.IsNullOrEmpty(fileId))
            {
                logService.LogInformation($"Send image from stream: BotId={botId}; ChatId={chatId}; Img={image}");
                byte[] imgByte = await GetImageByteArrayAsync(image);
                using MemoryStream stream = new(imgByte);
                result = await telegramBotService(botId).SendImageStreamAsync(botId, chatId, stream);
                if (result is not null)
                {
                    await cacheService.SetAsync(GetKeyCacheFileId(image, botId), result.Photo.FirstOrDefault()?.FileId, new CacheItemOptions
                    {
                        RelativeExpiration = TimeSpan.FromMinutes(60)
                    });
                }
            }
            else
            {
                result = await telegramBotService(botId).SendImageAsync(botId, chatId, fileId);
                logService.LogInformation($"Send image from fileId: BotId={botId}; ChatId={chatId}; FileId={fileId}");
            }
            return result;
        }

        private static string GetKeyCacheFileId(string imageUrl, long botId) => $"{imageUrl}?botId={botId}";

        private async Task<byte[]> GetImageByteArrayAsync(string imgUrl)
        {
            byte[] imgByte = await cacheService.GetAsync<byte[]>(imgUrl);
            if (imgByte is null)
            {
                string url = string.IsNullOrWhiteSpace(BaseImageUrl) || string.IsNullOrWhiteSpace(BaseImageServiceNameUrl) ? imgUrl : imgUrl.Replace(BaseImageUrl, BaseImageServiceNameUrl);
                imgByte = await botCommonService.ReadImageAsByteAsync(url);
                await cacheService.SetAsync(imgUrl, imgByte, new CacheItemOptions
                {
                    RelativeExpiration = TimeSpan.FromMinutes(10),
                });
            }
            return imgByte;
        }

        private async Task<SendNotificationResult> SendImagesOnlyTelegramService(AnnouncementNotification announcementNotification)
        {
            var result = new SendNotificationResult
            {
                AnnouncementId = announcementNotification.AnnouncementId,
                ConversationId = announcementNotification.ConversationId,
            };

            if (announcementNotification.RetryImages is not null && announcementNotification.RetryImages.Count > 0)
            {
                foreach (var image in announcementNotification.RetryImages)
                {
                    await SendImageTelegramService(announcementNotification, image, result);
                }
            }
            return result;
        }

        private IActionResult HandleExceptionVerifyGroup(Exception exception)
        {
            return exception.Message.Contains("bot was kicked from the supergroup chat", StringComparison.InvariantCultureIgnoreCase)
               || exception.Message.Contains("bot was kicked from the group chat", StringComparison.InvariantCultureIgnoreCase)
               || exception.Message.Contains("chat not found", StringComparison.InvariantCultureIgnoreCase)
                ? Ok(new
                {
                    IsBotRemoved = true,
                    Telegrams = new List<object>()
                })
                : (IActionResult)Ok(null);
        }

        private async Task WriteLogAnnouncementMessageAsync(AnnouncementNotification announcementNotification, string content, bool isBotBlocked, bool isSuccess, string errMsg)
        {
            var writeLogCommand = new WriteLogMessageCommand
            {
                ReferenceId = announcementNotification.AnnouncementId,
                DataSource = LogDataSource.Announcement,
                BotId = announcementNotification.BotId,
                ConversationId = announcementNotification.ConversationId,
                Content = content,
                IsSuccess = isSuccess,
                IsBotBlocked = isBotBlocked,
                ErrMsg = errMsg
            };
            await botCommonService.WriteLogMessageAsync(writeLogCommand);
        }

        private static void HandleExceptionTelegramMessage(SendNotificationResult sendResult, Exception exception)
        {
            sendResult.IsSuccessful = false;
            sendResult.ErrMsg = TelegramBotServiceErrorMessage;

            if (exception.Message.Contains("Message is too long", StringComparison.InvariantCultureIgnoreCase))
            {
                sendResult.ErrMsg = ErrorMessageTelegram.MessageIsTooLong;
                sendResult.IgnoredRetry = true;
            }

            if (exception.Message.Contains("bot was kicked from the supergroup chat", StringComparison.InvariantCultureIgnoreCase)
                || exception.Message.Contains("bot was kicked from the group chat", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                sendResult.ErrMsg = ErrorMessageTelegram.GroupChatDeleted;
                sendResult.IgnoredRetry = true;
            }

            if (exception.Message.Contains("chat not found", StringComparison.InvariantCultureIgnoreCase) ||
                exception.Message.Contains("the group chat was deleted", StringComparison.InvariantCultureIgnoreCase))
            {
                sendResult.ErrMsg = ErrorMessageTelegram.GroupNotFound;
                sendResult.IgnoredRetry = true;
            }

            if (exception.Message.Contains("bot was blocked by the user", StringComparison.InvariantCultureIgnoreCase))
            {
                sendResult.ErrMsg = ErrorMessageTelegram.BlockBot;
                sendResult.IgnoredRetry = true;
                sendResult.IsBotBlocked = true;
            }
        }
    }
}