namespace CRM.BotService.Core.Models.Letstalk
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class MessageMarkup
    {
        public const string Button = "button";

        public string Type { get; set; } = Button;

        [JsonProperty(PropertyName = "actions")]
        public IEnumerable<MessageAction> MessageActions { get; set; }
    }
}