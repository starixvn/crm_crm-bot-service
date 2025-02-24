using Newtonsoft.Json;

namespace CRM.BotService.Models.Letstalk;

public class MessageAction
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Data { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string Uri { get; set; }
}