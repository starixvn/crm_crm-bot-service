using System.Collections.Generic;

namespace CRM.BotService.Models.Notification;

public class SendNotificationResult
{
    public int AnnouncementId { get; set; }

    public string ConversationId { get; set; }

    public bool IsSuccessful { get; set; }

    public bool IsBotBlocked { get; set; }

    public bool IsOnlyRetryImage { get; set; }

    public bool IgnoredRetry { get; set; }

    public List<string> ErrImages { get; set; } = [];

    public string ErrMsg { get; set; }
}