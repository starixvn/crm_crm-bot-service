namespace CRM.BotService.Core.Models
{
    public class WebSettings
    {
        public string AppId { get; set; }
        public string Secret { get; set; }
        public string AppToken { get; set; }

        public int ExpirationByDay { get; set; } = 7;
    }
}