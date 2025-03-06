using Newtonsoft.Json;

namespace CRM.BotService.Models;

public class EventSource
{
    [JsonProperty(PropertyName = "user_id")]
    public string UserId { get; set; }

    [JsonProperty(PropertyName = "group_id")]
    public string GroupId { get; set; }
}