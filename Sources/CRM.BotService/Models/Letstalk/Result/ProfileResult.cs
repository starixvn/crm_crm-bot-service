using Newtonsoft.Json;

namespace CRM.BotService.Models.Letstalk.Result;

public class ProfileResult : BaseResult
{
    [JsonProperty(PropertyName = "avatar_url")]
    public string AvatarUrl { get; set; }

    [JsonProperty(PropertyName = "display_name")]
    public string DisplayName { get; set; }
}