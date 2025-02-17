namespace CRM.BotService.Core.Models.Letstalk.Results
{
    using Newtonsoft.Json;

    public class WebhookResult : BaseResult
    {
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
    }
}