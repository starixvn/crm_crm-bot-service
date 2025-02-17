namespace CRM.BotService.Core.Models.Letstalk.Results
{
    using Newtonsoft.Json;

    public class PublicKeyResult : BaseResult
    {
        [JsonProperty(PropertyName = "public_key")]
        public string PublicKey { get; set; }
    }
}