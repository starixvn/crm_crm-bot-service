using Newtonsoft.Json;
using System.Collections.Generic;

namespace CRM.BotService.Models.Letstalk.Result;

public class PushMessageResult : BaseResult
{
    [JsonProperty(PropertyName = "failList")]
    public List<string> FailList { get; set; } = [];

    [JsonProperty(PropertyName = "blockList")]
    public List<string> BlockList { get; set; } = [];

    public List<GlobalIdObject> GlobalIds { get; set; } = [];
}

public abstract class GlobalIdObject
{
    public string Receiver { get; set; }

    public string GlobalId { get; set; }
}