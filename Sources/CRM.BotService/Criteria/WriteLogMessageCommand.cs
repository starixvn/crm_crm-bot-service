using CRM.BotService.Constants;
using Fanex.Data.Repository;

namespace CRM.BotService.Criteria;

public class WriteLogMessageCommand : NonQueryCommand
{
    public int ReferenceId { get; set; }

    public string DataSource { get; set; } = LogDataSource.Notification;

    public long BotId { get; set; }

    public string ConversationId { get; set; }

    public string Content { get; set; }

    public bool IsBotBlocked { get; set; }

    public bool IsSuccess { get; set; }

    public string ErrMsg { get; set; }

    public override string GetSettingKey() => "Acc_CRM_ChatBot_NotificationMsgLog_Ins";

    public override bool IsValid() => BotId > 0 &&
                                      ReferenceId > 0 &&
                                      !string.IsNullOrWhiteSpace(ConversationId) &&
                                      !string.IsNullOrWhiteSpace(Content);
}