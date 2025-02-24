namespace CRM.BotService.Models;

public class HttpClientSetting
{
    public bool IsUseProxy { get; set; }

    public string ProxyAddress { get; set; }

    public bool BypassOnLocal { get; set; }

    public int TimeOut { get; set; }
}