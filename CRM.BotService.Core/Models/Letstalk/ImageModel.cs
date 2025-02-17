namespace CRM.BotService.Core.Models.Letstalk
{
    using Newtonsoft.Json;

    public class ImageModel
    {
        [JsonProperty(PropertyName = "original_image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty(PropertyName = "preview_image_url")]
        public string ImagePreviewUrl { get; set; }
    }
}