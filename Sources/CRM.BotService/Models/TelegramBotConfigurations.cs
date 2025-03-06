using Telegram.Bot;

namespace CRM.BotService.Models;

public class TelegramBotConfigurations
{
    public long BotId { get; set; }

    public string Token { get; set; }

    public ITelegramBotClient BotClient { get; set; }

    public string Domain { get; set; }
}