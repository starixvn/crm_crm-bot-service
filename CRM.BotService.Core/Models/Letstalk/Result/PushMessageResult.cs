namespace CRM.BotService.Core.Models.Letstalk.Results
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class PushMessageResult : BaseResult
    {
        [JsonProperty(PropertyName = "failList")]
        public List<string> FailList { get; set; } = new List<string>();

        [JsonProperty(PropertyName = "blockList")]
        public List<string> BlockList { get; set; } = new List<string>();

        public List<GlobalIdObject> GlobalIds { get; set; } = new List<GlobalIdObject>();
    }

    public class GlobalIdObject
    {
        public string Receiver { get; set; }
        public string GlobalId { get; set; }
    }
}