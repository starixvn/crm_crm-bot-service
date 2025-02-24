using Newtonsoft.Json;

namespace CRM.BotService.Models.Letstalk.Result;

public class WebhookResult : BaseResult
{
    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }
}