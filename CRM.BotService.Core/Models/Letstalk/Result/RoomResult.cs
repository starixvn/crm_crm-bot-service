namespace CRM.BotService.Core.Models.Letstalk.Results
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class RoomResult : BaseResult
    {
        [JsonProperty(PropertyName = "room")]
        public RoomData Room { get; set; }
    }

    public class RoomData
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "members")]
        public List<string> Members { get; set; }
    }
}