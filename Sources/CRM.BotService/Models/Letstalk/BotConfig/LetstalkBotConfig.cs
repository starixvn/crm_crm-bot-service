namespace CRM.BotService.Models.Letstalk.BotConfig;

public class LetstalkBotConfig
{
    public string ApiEndpoint { get; set; }

    public string ApiKey { get; set; }

    public string SecureKey { get; set; }

    public string ChatKey { get; set; }

    public string DisplayName { get; set; }

    public int BotAccountId { get; set; }

    public string BotUserId { get; set; }

    public int ChatKeyVersion { get; set; }

    public int BusinessAccountType { get; set; }

    public int EncryptionMode { get; set; }
}