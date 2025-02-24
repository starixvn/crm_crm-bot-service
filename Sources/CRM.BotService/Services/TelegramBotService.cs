using CRM.BotService.Constants;
using CRM.BotService.Criteria;
using CRM.BotService.Models;
using CRM.BotService.Models.Base;
using CRM.BotService.Services.Interfaces;
using Fanex.Logging.Extensions;
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

namespace CRM.BotService.Services;

public class TelegramBotService : ITelegramBotService
{
    private readonly IList<TelegramBotConfigurations> botConfigurations;
    private readonly ILogServiceExtend logService;
    private readonly IBotCommonService botCommonService;
    private const string TelegramBotServiceErrorMessage = "Error occurred on telegram chatbot service";

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
        await bot.BotClient.DeleteWebhook();
        await bot.BotClient.SetWebhook(webhook);
    }

    public async Task<User> GetBotInfoAsync(long botId)
    {
        var bot = botConfigurations.First(x => x.BotId == botId);
        return await bot.BotClient.GetMe();
    }

    public async Task<WebhookInfo> GetWebhookInfoAsync(TelegramBotConfigurations bot)
    {
        logService.LogInformation("Start GetWebhookInfoAsync: " + bot.BotId);
        return await bot.BotClient.GetWebhookInfo();
    }

    public async Task<Chat> GetChatAsync(long botId, long chatId)
    {
        var bot = botConfigurations.First(x => x.BotId == botId);
        var chat = await bot.BotClient.GetChat(new(chatId));
        return chat;
    }

    public async Task<ChatMember> GetChatMemberAsync(long botId, long chatGroupId, long chatMemberId)
    {
        try
        {
            var bot = botConfigurations.First(x => x.BotId == botId);
            var chatMember = await bot.BotClient.GetChatMember(new(chatGroupId), chatMemberId);
            return chatMember;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SendNotifyAsync(NotificationMessage notificationMessage, SendResult sendResult)
    {
        await SendTextMessageAsync(notificationMessage, sendResult);
        sendResult.SentTime = DateTimeOffset.UtcNow.DateTime;

        await WriteLogCommandAsync(notificationMessage, sendResult);
    }

    private async Task SendTextMessageAsync(NotificationMessage notificationMessage, SendResult sendResult)
    {
        try
        {
            _ = long.TryParse(notificationMessage.ConversationId, out var chatId);
            ValidateInputParam(nameof(notificationMessage.Content), notificationMessage.Content);
            var result = await SendTextMessageAsync(notificationMessage.BotId, chatId, notificationMessage.Content);
            if (result is not null)
            {
                sendResult.ErrorCode = 0;
            }
        }
        catch (Exception exception)
        {
            WriteLogInfo(notificationMessage, exception);
            HandleExceptionTelegramMessage(sendResult, exception);
        }
    }

    public async Task<Message> SendTextMessageAsync(long botId, long chatId, string messageContent, ParseMode parseMode = ParseMode.Html)
    {
        var bot = botConfigurations.First(x => x.BotId == botId);
        var messageContentReplace = RemoveUnsupportedTags(messageContent);

        ValidateInputParam(nameof(messageContent), messageContentReplace);
        var message = await bot.BotClient.SendMessage(new(chatId), messageContentReplace, parseMode: parseMode);
        return message;
    }

    public async Task<Message> SendImageAsync(long botId, long chatId, string urlImage)
    {
        var bot = botConfigurations.First(x => x.BotId == botId);

        var photo = new InputFileUrl(urlImage);
        var message = await bot.BotClient.SendPhoto(
            chatId: chatId,
            photo: photo);
        return message;
    }

    public async Task<Message> SendImageStreamAsync(long botId, long chatId, Stream stream)
    {
        var bot = botConfigurations.First(x => x.BotId == botId);

        var photo = new InputFileStream(stream);
        var message = await bot.BotClient.SendPhoto(
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

    private void WriteLogInfo(NotificationMessage notificationMessage, Exception exception)
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
            || (paramValue is string value && string.IsNullOrWhiteSpace(value)))
        {
            throw new ArgumentNullException(paramName);
        }
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
}