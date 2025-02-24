namespace CRM.BotService.Constants;

public static class ErrorMessageTelegram
{
    public const string MessageIsTooLong = "The messages length exceeds 4,096 characters.";
    public const string DisableChannelErrorMessage = "Chatbot service has been stopped due to turning off the configuration by admin.";
    public const string GroupChatDeleted = "The Chatbot has been removed by users.";
    public const string GroupNotFound = "The group has been deleted.";
    public const string BlockBot = "N/A";
}