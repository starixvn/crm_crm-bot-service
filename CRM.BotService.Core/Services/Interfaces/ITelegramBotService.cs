namespace CRM.BotService.Core.Services.Interfaces
{
    using Models;
    using Models.Base;
    using System.IO;
    using System.Threading.Tasks;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;

    public interface ITelegramBotService
    {
        Task SetWebhookAsync(TelegramBotConfigurations bot);

        Task<WebhookInfo> GetWebhookInfoAsync(TelegramBotConfigurations bot);

        Task<User> GetBotInfoAsync(long botId);

        Task<ChatMember> GetChatMemberAsync(long botId, long chatGroupId, long chatMemberId);

        Task<Chat> GetChatAsync(long botId, long chatId);

        Task<Message> SendTextMessageAsync(long botId, long chatId, string messageContent, ParseMode parseMode = ParseMode.Html);

        Task SendNotifyAsync(NotificationMessage notificationMessage, SendResult sendResult);

        Task<Message> SendImageAsync(long botId, long chatId, string urlImage);

        Task<Message> SendImageStreamAsync(long botId, long chatId, Stream stream);
    }
}