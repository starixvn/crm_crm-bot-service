namespace CRM.BotService.Services.Telegram.Models.Result
{
    public class PushMessageTelegramResult
    {
        public int NotificationId { get; set; }

        public string ConversationId { get; set; }

        public DateTime SentTime { get; set; }

        public int ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsIgnoreRetry { get; set; }
    }
}
