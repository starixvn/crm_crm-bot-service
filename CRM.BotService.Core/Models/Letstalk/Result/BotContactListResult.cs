namespace CRM.BotService.Core.Models.Letstalk.Results
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class BotContactListResult : BaseResult
    {
        [JsonProperty(PropertyName = "contacts")]
        public List<string> Contacts { get; set; }

        public int Count { get; set; }
    }
}