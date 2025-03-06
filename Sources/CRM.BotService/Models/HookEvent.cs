using CRM.BotService.Models.Letstalk;
using Newtonsoft.Json;

namespace CRM.BotService.Models;

public class HookEvent
{
    public string Type { get; set; }

    [JsonProperty(PropertyName = "payload")]
    public string EncodedPayload { get; set; }

    public Payload PayloadObject { get; set; }

    public EventSource Source { get; set; }
}