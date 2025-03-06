using CRM.BotService.Models;

namespace CRM.BotService.Criteria;

public class UpdateGroupNameCommand : BaseNonQueryCommand
{
    public string ConversationId { get; set; }

    public string DisplayName { get; set; }

    public string UpdatedBy { get; set; }

    public short ChannelId { get; set; }

    public override string GetSettingKey() => "Acc_CRM_ChatBot_ChangeGroupName_Upd";

    public override bool IsValid() => !string.IsNullOrWhiteSpace(ConversationId) &&
                                      !string.IsNullOrWhiteSpace(DisplayName);
}