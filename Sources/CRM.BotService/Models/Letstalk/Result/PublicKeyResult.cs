using Newtonsoft.Json;

namespace CRM.BotService.Models.Letstalk.Result;

public class PublicKeyResult : BaseResult
{
    [JsonProperty(PropertyName = "public_key")]
    public string PublicKey { get; set; }
}