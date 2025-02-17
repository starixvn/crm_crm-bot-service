namespace CRM.BotService.Core.Models.Letstalk
{
    using Newtonsoft.Json;

    public class Payload
    {
        public int Id { get; set; }

        [JsonProperty(PropertyName = "hash_content_key")]
        public string HashContentKey { get; set; }

        public string Type { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        public string Receiver { get; set; }

        public long Time { get; set; }

        [JsonProperty(PropertyName = "send_time")]
        public long SendTime => Time;

        [JsonProperty(PropertyName = "due_date")]
        public long DueDate { get; set; }

        public int From { get; set; }

        [JsonProperty(PropertyName = "hash_content")]
        public string HashContent { get; set; }

        [JsonProperty(PropertyName = "hash_id")]
        public int HashId { get; set; }

        [JsonProperty(PropertyName = "hash_type")]
        public int HashType { get; set; }
    }
}