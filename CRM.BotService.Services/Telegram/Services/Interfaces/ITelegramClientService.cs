namespace CRM.BotService.Services.Telegram.Services.Interfaces
{
    using CRM.BotService.Services.Telegram.Models.Result;

    public interface ITelegramClientService
    {
        Task<PushMessageTelegramResult> PushPersonalByTelegramAsync(long botId, long chatIds, string message);
    }
}
