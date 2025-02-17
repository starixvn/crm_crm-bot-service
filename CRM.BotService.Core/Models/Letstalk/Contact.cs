namespace CRM.BotService.Core.Models.Letstalk
{
    using Newtonsoft.Json;

    public class Contact
    {
        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }
    }
}