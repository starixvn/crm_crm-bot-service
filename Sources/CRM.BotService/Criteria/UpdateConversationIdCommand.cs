using CRM.BotService.Models;

namespace CRM.BotService.Criteria;

public class UpdateConversationIdCommand : BaseNonQueryCommand
{
    public string NewConversationId { get; set; }

    public string OldConversationId { get; set; }

    public string UpdatedBy { get; set; }

    public short ChannelId { get; set; }

    public override string GetSettingKey() => "Acc_CRM_ChatBot_ConversationId_Upd";

    public override bool IsValid() => !string.IsNullOrWhiteSpace(NewConversationId)
                                      && !string.IsNullOrWhiteSpace(OldConversationId);
}