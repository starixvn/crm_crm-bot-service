using CRM.BotService.Models;

namespace CRM.BotService.Criteria;

public class RemoveBotFromGroupCommand : BaseNonQueryCommand
{
    public long BotId { get; set; }

    public string ConversationId { get; set; }

    public short ChannelId { get; set; }

    public string UpdatedBy { get; set; }

    public override string GetSettingKey() => "Acc_CRM_ChatBot_KickOutBotFromGroup_Del";

    public override bool IsValid() => BotId > 0 && !string.IsNullOrWhiteSpace(ConversationId);
}