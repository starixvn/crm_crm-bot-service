namespace CRM.BotService.Core.Models.Base
{
    using System;

    public class SendResult
    {
        public int NotificationId { get; set; }
        public int ConversationGroupId { get; set; }
        public DateTime SentTime { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccessful => ErrorCode == 0;
        public bool IsIgnoreRetry { get; set; }
        public bool IsBlockedBot { get; set; }
    }
}