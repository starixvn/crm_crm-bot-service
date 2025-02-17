namespace CRM.BotService.Core.Services
{
    using Constants;
    using Constants.Notification;
    using Criteria;
    using Enum;
    using Fanex.Logging.Extensions;
    using Interfaces;
    using Models;
    using Models.Base;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Telegram.Bot;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;

    public class TelegramBotService : ITelegramBotService
    {
        private readonly IList<TelegramBotConfigurations> botConfigurations;
        private readonly ILogServiceExtend logService;
        private readonly IBotCommonService botCommonService;

        private static readonly string TelegramBotServiceErrorMessage = string.Format(SendNotificationErrorMessage.BotServiceErrorMsg, BotChannel.Telegram.ToString().ToLowerInvariant());

        public TelegramBotService(
            IHttpClientFactory clientFactory,
            List<TelegramBotConfigurations> botConfigurations,
            ILogServiceExtend logService,
            IBotCommonService botCommonService)
        {
            this.botConfigurations = botConfigurations;
            this.logService = logService;
            this.botCommonService = botCommonService;

            var httpClient = clientFactory.CreateClient(NamedHttpClients.ProxiedClient);
            foreach (var bot in botConfigurations)
            {
                bot.BotClient = new TelegramBotClient(bot.Token, httpClient);
            }
        }

        public async Task SetWebhookAsync(TelegramBotConfigurations bot)
        {
            ValidateInputParam(nameof(bot.Domain), bot.Domain);
            var webhook = $"{bot.Domain}api/telegram/webhook/{bot.BotId}/update";
            await bot.BotClient.DeleteWebhookAsync();
            await bot.BotClient.SetWebhookAsync(webhook);
        }

        public async Task<User> GetBotInfoAsync(long botId)
        {
            TelegramBotConfigurations bot = botConfigurations.First(x => x.BotId == botId);
            return await bot.BotClient.GetMeAsync();
        }

        public async Task<WebhookInfo> GetWebhookInfoAsync(TelegramBotConfigurations bot)
        {
            logService.LogInformation("Start GetWebhookInfoAsync: " + bot.BotId);
            return await bot.BotClient.GetWebhookInfoAsync();
        }

        public async Task<Chat> GetChatAsync(long botId, long chatId)
        {
            TelegramBotConfigurations bot = botConfigurations.First(x => x.BotId == botId);
            var chat = await bot.BotClient.GetChatAsync(new ChatId(chatId));
            return chat;
        }

        public async Task<ChatMember> GetChatMemberAsync(long botId, long chatGroupId, long chatMemberId)
        {
            try
            {
                TelegramBotConfigurations bot = botConfigurations.First(x => x.BotId == botId);
                var chatMember = await bot.BotClient.GetChatMemberAsync(new ChatId(chatGroupId), chatMemberId);
                return chatMember;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task SendNotifyAsync(NotificationMessage notificationMessage, SendResult sendResult)
        {
            logService.LogInformation("Start SendNotifyAsync: " + notificationMessage.ConversationId);
            await SendTextMessageAsync(notificationMessage, sendResult);
            sendResult.SentTime = DateTimeOffset.UtcNow.DateTime;
            await WriteLogCommandAsync(notificationMessage, sendResult);
            logService.LogInformation("End SendNotifyAsync: " + notificationMessage.ConversationId);
        }

        public async Task SendTextMessageAsync(NotificationMessage notificationMessage, SendResult sendResult)
        {
            try
            {
                _ = long.TryParse(notificationMessage.ConversationId, out long chatId);
                ValidateInputParam(nameof(notificationMessage.Content), notificationMessage.Content);
                var result = await SendTextMessageAsync(notificationMessage.BotId, chatId, notificationMessage.Content);
                if (result is not null)
                {
                    sendResult.ErrorCode = 0;
                }
            }
            catch (Exception exception)
            {
                WriteLogInfor(notificationMessage, exception);
                HandleExceptionTelegramMessage(sendResult, exception);
            }
        }

        public async Task<Message> SendTextMessageAsync(long botId, long chatId, string messageContent, ParseMode parseMode = ParseMode.Html)
        {
            TelegramBotConfigurations bot = botConfigurations.First(x => x.BotId == botId);
            var messageContentReplace = RemoveUnsupportedTags(messageContent);

            ValidateInputParam(nameof(messageContent), messageContentReplace);
            var message = await bot.BotClient.SendTextMessageAsync(new ChatId(chatId), messageContentReplace, parseMode);
            return message;
        }

        public async Task<Message> SendImageAsync(long botId, long chatId, string urlImage)
        {
            TelegramBotConfigurations bot = botConfigurations.First(x => x.BotId == botId);

            var photo = new Telegram.Bot.Types.InputFiles.InputOnlineFile(urlImage);
            Message message = await bot.BotClient.SendPhotoAsync(
                chatId: chatId,
                photo: photo);
            return message;
        }

        public async Task<Message> SendImageStreamAsync(long botId, long chatId, Stream stream)
        {
            TelegramBotConfigurations bot = botConfigurations.First(x => x.BotId == botId);

            var photo = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream);
            Message message = await bot.BotClient.SendPhotoAsync(
                chatId: chatId,
                photo: photo);
            return message;
        }

        public async Task WriteLogCommandAsync(NotificationMessage notificationMessage, SendResult sendResult, string image = "")
        {
            var writeLogCommand = new WriteLogMessageCommand
            {
                ReferenceId = notificationMessage.NotificationId,
                BotId = notificationMessage.BotId,
                ConversationId = notificationMessage.ConversationId,
                Content = string.IsNullOrEmpty(image) ? notificationMessage.Content : image,
                IsSuccess = sendResult.IsSuccessful,
                IsBotBlocked = sendResult.IsBlockedBot,
                ErrMsg = sendResult.IsSuccessful ? string.Empty : sendResult.ErrorMessage
            };
            await botCommonService.WriteLogMessageAsync(writeLogCommand);
        }

        private static void HandleExceptionTelegramMessage(SendResult sendResult, Exception exception)
        {
            sendResult.ErrorCode = 1;
            sendResult.ErrorMessage = TelegramBotServiceErrorMessage;

            if (exception.Message.Contains("Message is too long", StringComparison.InvariantCultureIgnoreCase))
            {
                sendResult.ErrorMessage = ErrorMessageTelegram.MessageIsTooLong;
                sendResult.IsIgnoreRetry = true;
            }

            if (exception.Message.Contains("bot was kicked from the supergroup chat", StringComparison.InvariantCultureIgnoreCase)
                || exception.Message.Contains("bot was kicked from the group chat", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                sendResult.ErrorMessage = ErrorMessageTelegram.GroupChatDeleted;
                sendResult.IsIgnoreRetry = true;
            }

            if (exception.Message.Contains("chat not found", StringComparison.InvariantCultureIgnoreCase)
                || exception.Message.Contains("the group chat was deleted", StringComparison.InvariantCultureIgnoreCase))
            {
                sendResult.ErrorMessage = ErrorMessageTelegram.GroupNotFound;
                sendResult.IsIgnoreRetry = true;
            }

            if (exception.Message.Contains("bot was blocked by the user", StringComparison.InvariantCultureIgnoreCase))
            {
                sendResult.ErrorMessage = ErrorMessageTelegram.BlockBot;
                sendResult.IsIgnoreRetry = true;
                sendResult.IsBlockedBot = true;
            }
        }

        public void WriteLogInfor(NotificationMessage notificationMessage, Exception exception)
        {
            var param = new Dictionary<string, object>
            {
                { "notificationId", notificationMessage.NotificationId },
                { "botId", notificationMessage.BotId },
                { "chatId", notificationMessage.ConversationId }
            };
            var logMessage = new LogMessage(exception.Message)
                .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
                .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
                .AddSection("Params", param);

            logService.LogInformation(logMessage.ToString());
        }

        private static void ValidateInputParam(string paramName, object paramValue)
        {
            if (paramValue == null
                || (paramValue is string && string.IsNullOrWhiteSpace(paramValue as string)))
            {
                throw new ArgumentNullException(paramName);
            }
        }

        private static string RemoveUnsupportedTags(string content)
        {
            string[] allowedTagsHtml = { "b", "strong", "i", "em", "ins", "u", "s", "strike", "del", "a", "tg-emoji", "code", "pre", "br" };
            var regex = new Regex(@"(<br />|<hr />)");
            var replaceNewLine = regex.Replace(content, Environment.NewLine);
            string messageContentReplace = Regex.Replace(replaceNewLine, "<(?!/?(" + string.Join("|", allowedTagsHtml) + ")(>|\\s.*?>)).*?>", "");
            return messageContentReplace;
        }
    }
}