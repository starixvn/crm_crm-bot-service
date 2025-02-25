namespace CRM.BotService.Constants;

public static class CommandMessageTelegram
{
    public const string Welcome = "Welcome to <b>{0}</b> service. You have successfully added the bot, and your ChatID is <b>{1}</b>." +
                                   " You are almost done with the setup. Please contact Customer Support Department for support. Thank you!";

    public const string BotAddDoNotHaveUserName = "Thank you for choosing <b>{0}</b> service. To receive our notifications, please create a username for your account " +
                                                  @"and then send the <b>""/start""</b> command via the chat box. If you need any assistance, please contact Customer Service Department. Thank you!";

    public const string AddBotToGroupGreetingMessage = "Welcome to <b>{0}</b> service, which sends various types of notifications to our valued customers. " +
                                                       "The bot has been added successfully with the ChatID <b>{1}</b>. To receive the notifications, " +
                                                       "Customer Service Department needs to set up further steps. Thank you!";

    public const string SendDefaultMessage = "Dear valued customers, this bot is used to send notifications." +
                                             " If you need any assistance, please contact the Customer Service Department. Thank you!";
}