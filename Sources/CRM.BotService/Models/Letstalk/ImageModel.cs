using Newtonsoft.Json;

namespace CRM.BotService.Models.Letstalk;

public class ImageModel
{
    [JsonProperty(PropertyName = "original_image_url")]
    public string ImageUrl { get; set; }

    [JsonProperty(PropertyName = "preview_image_url")]
    public string ImagePreviewUrl { get; set; }
}