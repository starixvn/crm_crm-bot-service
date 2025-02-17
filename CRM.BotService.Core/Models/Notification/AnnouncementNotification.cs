namespace CRM.BotService.Core.Models.Notification
{
    using Enum;
    using System.Collections.Generic;

    public class AnnouncementNotification
    {
        public BotChannel ChannelId { get; set; }
        public int AnnouncementId { get; set; }
        public int NotificationId { get; set; }
        public int ConversationGroupId { get; set; }
        public string Content { get; set; }
        public long BotId { get; set; }
        public string ConversationId { get; set; }
        public string FileName { get; set; }
        public List<string> Images { get; set; }
        public bool IsOnlyRetryImage { get; set; }
        public string ErrImages { get; set; }
        public List<string> RetryImages { get; set; }
        public ChatType ChatTypeId { get; set; }
    }
}