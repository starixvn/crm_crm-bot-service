namespace CRM.BotService.Endpoint.Controllers.Telegram;

using Constants;
using Criteria;
using Enum;
using global::Telegram.Bot.Types;
using global::Telegram.Bot.Types.Enums;
using Microsoft.AspNetCore.Mvc;
using Models;
using Newtonsoft.Json;
using Services;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class UpdateController(
    ILogServiceExtend logService,
    Func<long, ITelegramBotService> telegramBotService,
    IBaseService baseService,
    List<TelegramBotConfigurations> botConfigurations)
    : Controller
{
    [HttpPost]
    [Route("api/telegram/webhook/{botId:long}/update")]
    public async Task<IActionResult> Post(long botId, [FromBody] Update update)
    {
        try
        {

            if (update is null || update.Type != UpdateType.Message)
            {
                return Ok();
            }
            var bot = await telegramBotService(botId).GetBotInfoAsync(botId);
            if (bot is null)
            {
                await logService.ErrorMessageAsync($"Telegram Bot {botId} is invalid.");
                return Ok($"Telegram Bot {botId} is invalid.");
            }
            await HandleEventUpdate(bot, update.Message);
            return Ok();
        }
        catch (Exception ex)
        {
            await logService.ErrorAsync(ex);
            return Ok(ex.Message);
        }
    }

    private async Task HandleEventUpdate(User bot, Message message)
    {
        switch (message.Type)
        {
            case MessageType.Text:
                await HandleBotCommandAsync(bot, message);
                break;

            case MessageType.ForumTopicEdited:
                await EditChatTitledAsync(message);
                break;

            case MessageType.ForumTopicCreated:
                await AddBotToGroupAsync(message, bot);
                break;

            case MessageType.NewChatMembers:
                if (message.NewChatMembers != null)
                {
                    var bots = message.NewChatMembers.Where(x => x.IsBot).ToArray();
                    foreach (var t in bots)
                    {
                        await AddBotToGroupAsync(message, t, bot);
                    }
                }

                break;

            case MessageType.LeftChatMember:
                await RemoveBotToGroupAsync(message);
                break;

            case MessageType.SupergroupChatCreated:
                await MigratedToSupergroupAsync(message);
                break;

            default:
                await HandleSendDefaultTextAsync(bot, message);
                break;
        }
    }

    private async Task HandleBotCommandAsync(User bot, Message message)
    {
        var commands = message.Entities?.Where(entity => entity.Type == MessageEntityType.BotCommand)
            .Select(entity =>
                message.Text != null ? message.Text.Substring(entity.Offset, entity.Length) : string.Empty
                ).ToList();
        if (commands is null || commands.Count == 0)
        {
            await HandleSendDefaultTextAsync(bot, message);
            return;
        }
        foreach (var command in commands)
        {
            var isStartCommand = command.Equals("/start", StringComparison.CurrentCultureIgnoreCase);
            var isGetGroupIdCommand = command.Equals("/getgroupid", StringComparison.CurrentCultureIgnoreCase)
                                      || command.Equals($"/getgroupid@{bot.Username}", StringComparison.CurrentCultureIgnoreCase);
            if (isStartCommand && message.Chat.Type == global::Telegram.Bot.Types.Enums.ChatType.Private)
            {
                await AddBotToPersonalAsync(message, bot);
            }
            else if (isGetGroupIdCommand)
            {
                await telegramBotService(bot.Id).SendTextMessageAsync(bot.Id, message.Chat.Id, message.Chat.Id.ToString());
            }
            else
            {
                await HandleSendDefaultTextAsync(bot, message);
            }
        }
    }

    private async Task HandleSendDefaultTextAsync(User bot, Message message)
    {
        await telegramBotService(bot.Id).SendTextMessageAsync(bot.Id, message.Chat.Id, CommandMessageTelegram.SendDefaultMessage);
    }

    private async Task EditChatTitledAsync(Message message)
    {
        var command = new UpdateGroupNameCommand
        {
            ConversationId = message.Chat.Id.ToString(),
            DisplayName = message.NewChatTitle,
            ChannelId = (byte)BotChannel.Telegram,
            UpdatedBy = message.From?.Id.ToString()
        };
        var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);
        logService.LogInformation("start EditChatTitledAsync: " + commandSerialize);
        var result = await baseService.ExecuteNonQueryAsync(command);
        if (!result.IsSuccessful)
        {
            await logService.ErrorAsync(new(result.Message));
        }
        logService.LogInformation("end EditChatTitledAsync!!!");
    }

    private async Task AddBotToGroupAsync(Message message, User newBotAdd, User bot = null)
    {
        var isExistBot = newBotAdd != null && IsExistedBot(newBotAdd.Id);

        if (!isExistBot)
        {
            await logService.ErrorMessageAsync($"bot: {newBotAdd?.Id} does not exist");
            return;
        }

        var result = await CreateAddBotToGroupCommand(message, newBotAdd);

        if (result.IsSuccessful && (bot == null || bot.Id == newBotAdd.Id))
        {
            var greeting = string.Format(CommandMessageTelegram.AddBotToGroupGreetingMessage, newBotAdd.Username, message.Chat.Id);
            await telegramBotService(newBotAdd.Id).SendTextMessageAsync(newBotAdd.Id, message.Chat.Id, greeting);
        }
        else
        {
            await logService.ErrorAsync(new Exception(result.Message));
        }
    }

    private async Task<CommandResult> CreateAddBotToGroupCommand(Message message, User bot)
    {
        var command = new AddBotToGroupCommand
        {
            BotId = bot.Id,
            BotName = bot.Username,
            ConversationId = message.Chat.Id.ToString(),
            GroupName = message.Chat.Title,
            ChannelId = (byte)BotChannel.Telegram,
            UpdatedBy = message.From?.Id.ToString()
        };
        var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);
        logService.LogInformation("start AddBotToGroupAsync: " + commandSerialize);

        var result = await baseService.ExecuteNonQueryAsync(command);
        return result;
    }

    private async Task RemoveBotToGroupAsync(Message message)
    {
        var isExistBot = message.LeftChatMember != null && IsExistedBot(message.LeftChatMember.Id);
        if (isExistBot && message.LeftChatMember.IsBot)
        {
            var command = new RemoveBotFromGroupCommand
            {
                BotId = message.LeftChatMember.Id,
                ConversationId = message.Chat.Id.ToString(),
                ChannelId = (byte)BotChannel.Telegram,
                UpdatedBy = message.From?.Id.ToString()
            };
            var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);
            var result = await baseService.ExecuteNonQueryAsync(command);
            if (!result.IsSuccessful)
            {
                await logService.ErrorAsync(new($"Command: {commandSerialize}. Result message: {result.Message}"));
            }
        }
    }

    private async Task MigratedToSupergroupAsync(Message message)
    {
        var command = new UpdateConversationIdCommand
        {
            OldConversationId = message.Chat.Id.ToString(),
            NewConversationId = message.MigrateToChatId.ToString(),
            ChannelId = (byte)BotChannel.Telegram,
            UpdatedBy = message.From?.Id.ToString()
        };

        var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);

        logService.LogInformation("start MigratedToSupergroupAsync: " + commandSerialize);

        var result = await baseService.ExecuteNonQueryAsync(command);
        if (!result.IsSuccessful)
        {
            await logService.ErrorAsync(new(result.Message));
        }

        logService.LogInformation("end MigratedToSupergroupAsync!!!");
    }

    private async Task AddBotToPersonalAsync(Message message, User bot)
    {
        logService.LogInformation("start AddBotToPersonal!!!");
        if (string.IsNullOrEmpty(message.Chat.Username))
        {
            logService.LogInformation("empty username !!!");
            var errorMessageNotify = string.Format(CommandMessageTelegram.BotAddDoNotHaveUserName, bot.Username);
            await telegramBotService(bot.Id).SendTextMessageAsync(bot.Id, message.Chat.Id, errorMessageNotify);
        }
        else
        {
            var command = new AddBotToPersonalCommand
            {
                BotId = bot.Id,
                BotName = bot.FirstName,
                ConversationId = message.Chat.Id.ToString(),
                Username = message.Chat.Username,
                ChannelId = (byte)BotChannel.Telegram,
                UpdatedBy = message.Chat.Id.ToString()
            };
            var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);
            logService.LogInformation("start AddBotToPersonal: " + commandSerialize);

            var result = await baseService.ExecuteNonQueryAsync(command);
            var greeting = string.Format(CommandMessageTelegram.Wellcome, bot.Username, message.Chat.Id);
            await telegramBotService(bot.Id).SendTextMessageAsync(bot.Id, message.Chat.Id, greeting);
            if (!result.IsSuccessful)
            {
                await logService.ErrorAsync(new(result.Message));
            }
        }

        logService.LogInformation("end AddBotToPersonal!!!");
    }

    private bool IsExistedBot(long botId) => botConfigurations.Exists(x => x.BotId == botId);
}