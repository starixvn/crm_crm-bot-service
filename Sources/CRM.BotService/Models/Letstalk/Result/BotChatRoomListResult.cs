using System.Collections.Generic;
using Newtonsoft.Json;

namespace CRM.BotService.Models.Letstalk.Result;

public class BotChatRoomListResult : BaseResult
{
    [JsonProperty(PropertyName = "rooms")]
    public List<string> Rooms { get; set; }

    public int Count { get; set; }
}