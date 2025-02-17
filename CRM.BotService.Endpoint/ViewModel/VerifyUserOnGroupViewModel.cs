namespace CRM.BotService.Endpoint.ViewModel
{
    using System.Collections.Generic;

    public class VerifyUserOnGroupViewModel
    {
        public long BotId { get; set; }
        public string ConversationGroupId { get; set; }
        public List<string> BotAccountIds { get; set; }
    }
}