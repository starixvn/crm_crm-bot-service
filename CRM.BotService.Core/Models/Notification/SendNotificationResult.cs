namespace CRM.BotService.Core.Models.Notification
{
    using System.Collections.Generic;

    public class SendNotificationResult
    {
        public int AnnouncementId { get; set; }
        public string ConversationId { get; set; }
        public bool IsSuccessful { get; set; }
        public bool IsBotBlocked { get; set; }
        public bool IsOnlyRetryImage { get; set; }
        public bool IgnoredRetry { get; set; }
        public List<string> ErrImages { get; set; } = new();
        public string ErrMsg { get; set; }
    }
}