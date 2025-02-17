namespace CRM.BotService.Core.Models
{
    using Telegram.Bot;

    public class TelegramBotConfigurations
    {
        public long BotId { get; set; }
        public string Token { get; set; }
        public ITelegramBotClient BotClient { get; set; }
        public string Domain { get; set; }
    }
}
