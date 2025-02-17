namespace CRM.BotService.Services.Telegram.Models.Result
{
    public class BaseResult
    {
        public int Status { get; set; }

        public bool IsSuccess => Status == 1;

        public string Message { get; set; } = string.Empty;
    }
}
