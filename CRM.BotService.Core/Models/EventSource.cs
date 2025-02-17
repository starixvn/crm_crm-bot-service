namespace CRM.BotService.Core.Models
{
    using Newtonsoft.Json;

    public class EventSource
    {
        public string Type { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "group_id")]
        public string GroupId { get; set; }
    }
}