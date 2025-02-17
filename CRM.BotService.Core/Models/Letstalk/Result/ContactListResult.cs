namespace CRM.BotService.Core.Models.Letstalk.Results
{
    using System.Collections.Generic;

    public class ContactListResult : BaseResult
    {
        public List<Contact> Contacts { get; set; }
    }
}