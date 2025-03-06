using System.Collections.Generic;
using Newtonsoft.Json;

namespace CRM.BotService.Models.Letstalk.Result;

public class BotContactListResult : BaseResult
{
    [JsonProperty(PropertyName = "contacts")]
    public List<string> Contacts { get; set; }

    public int Count { get; set; }
}