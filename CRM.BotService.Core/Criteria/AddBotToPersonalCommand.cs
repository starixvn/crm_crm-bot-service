namespace CRM.BotService.Core.Criteria
{
    using Models;

    public class AddBotToPersonalCommand : BaseNonQueryCommand
    {
        public long BotId { get; set; }
        public string BotName { get; set; }
        public string ConversationId { get; set; }
        public short ChannelId { get; set; }
        public string Username { get; set; }
        public string UpdatedBy { get; set; }

        public override string GetSettingKey() => "Acc_CRM_ChatBot_AddBotToPersonal_Ins";

        public override bool IsValid() => !string.IsNullOrWhiteSpace(ConversationId) &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(BotName) &&
            BotId > 0;
    }
}