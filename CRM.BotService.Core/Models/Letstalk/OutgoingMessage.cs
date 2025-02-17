namespace CRM.BotService.Core.Models.Letstalk
{
    using Enum;
    using Newtonsoft.Json;
    using System;
    using System.Text;

    public class OutgoingMessage
    {
        [JsonIgnore]
        public NotificationMessageType MessageType { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type
        {
            get
            {
                switch (MessageType)
                {
                    case NotificationMessageType.PlainText:
                        return MulticastType.Text;

                    case NotificationMessageType.Image:
                        return MulticastType.Image;

                    case NotificationMessageType.FullImage:
                        return MulticastType.FullImage;

                    case NotificationMessageType.Html:
                        return MulticastType.HTML;

                    default:
                        return MulticastType.Text;
                }
            }
        }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty(PropertyName = "base64Text")]
        public string Base64Text => !string.IsNullOrWhiteSpace(Text) && Type.Equals(MulticastType.Text, StringComparison.CurrentCultureIgnoreCase)
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes(Text)) : string.Empty;
    }

    public static class MulticastType
    {
        public const string Text = "text";
        public const string HTML = "html";
        public const string Image = "image";
        public const string FullImage = "fullimage";
    }
}