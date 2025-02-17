namespace CRM.BotService.Endpoint.Controllers.Telegram
{
    using Core.Constants;
    using Core.Criteria;
    using Core.Enum;
    using Core.Models;
    using Core.Services;
    using Core.Services.Interfaces;
    using global::Telegram.Bot.Types;
    using global::Telegram.Bot.Types.Enums;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class UpdateController : Controller
    {
        private readonly Func<long, ITelegramBotService> telegramBotService;
        private readonly ILogServiceExtend logService;
        private readonly IBaseService baseService;
        private readonly List<TelegramBotConfigurations> botConfigurations;

        public UpdateController(
            ILogServiceExtend logService,
            Func<long, ITelegramBotService> telegramBotService,
            IBaseService baseService,
            List<TelegramBotConfigurations> botConfigurations)
        {
            this.logService = logService;
            this.telegramBotService = telegramBotService;
            this.baseService = baseService;
            this.botConfigurations = botConfigurations;
        }

        [HttpPost]
        [Route("api/telegram/webhook/{botId:long}/update")]
        public async Task<IActionResult> Post(long botId, [FromBody] Update update)
        {
            try
            {
                logService.LogInformation(string.Format("Update webhook telegram: Type: {0}, ChatId: {1} ", update?.Type, update?.Message?.Chat?.Id));

                if (update is null || update.Type != UpdateType.Message)
                {
                    return Ok();
                }
                User bot = await telegramBotService(botId).GetBotInfoAsync(botId);
                if (bot is null)
                {
                    await logService.ErrorMessageAsync($"Telegram Bot {botId} is invalid.");
                    return Ok($"Telegram Bot {botId} is invalid.");
                }
                await HandleEventUpdate(bot, update.Message);
                logService.LogInformation("End webhook telegram!!!");
                return Ok();
            }
            catch (Exception ex)
            {
                await logService.ErrorAsync(ex);
                return Ok(ex.Message);
            }
        }

        protected internal virtual async Task HandleEventUpdate(User bot, Message message)
        {
            switch (message.Type)
            {
                case MessageType.Text:
                    await HandleBotCommandAsync(bot, message);
                    break;

                case MessageType.ChatTitleChanged:
                    await EditChatTitledAsync(message);
                    break;

                case MessageType.GroupCreated:
                    await AddBotToGroupAsync(message, bot);
                    break;

                case MessageType.ChatMembersAdded:
                    User[] bots = message.NewChatMembers.Where(x => x.IsBot).ToArray();
                    for (var i = 0; i < bots.Length; i++)
                    {
                        await AddBotToGroupAsync(message, bots[i], bot);
                    }
                    break;

                case MessageType.ChatMemberLeft:
                    await RemoveBotToGroupAsync(message, bot);
                    break;

                case MessageType.MigratedToSupergroup:
                    await MigratedToSupergroupAsync(message);
                    break;

                default:
                    await HandleSendDefaultTextAsync(bot, message);
                    break;
            }
        }

        protected internal virtual async Task HandleBotCommandAsync(User bot, Message message)
        {
            var commands = message.Entities?.Where(entity => entity.Type == MessageEntityType.BotCommand)
                .Select(entity => message.Text.Substring(entity.Offset, entity.Length)).ToList();
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

        protected internal virtual async Task HandleSendDefaultTextAsync(User bot, Message message)
        {
            await telegramBotService(bot.Id).SendTextMessageAsync(bot.Id, message.Chat.Id, CommandMessageTelegram.SendDefaultMessage);
        }

        protected internal virtual async Task<CommandResult> EditChatTitledAsync(Message message)
        {
            var command = new UpdateGroupNameCommand
            {
                ConversationId = message.Chat.Id.ToString(),
                DisplayName = message.NewChatTitle,
                ChannelId = (byte)BotChannel.Telegram,
                UpdatedBy = message.From.Id.ToString()
            };
            var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);
            logService.LogInformation("start EditChatTitledAsync: " + commandSerialize);
            var result = await baseService.ExecuteNonQueryAsync(command);
            if (!result.IsSuccessful)
            {
                await logService.ErrorAsync(new Exception(result.Message));
            }
            logService.LogInformation("end EditChatTitledAsync!!!");
            return result;
        }

        protected internal virtual async Task<CommandResult> AddBotToGroupAsync(Message message, User newBotAdd, User bot = null)
        {
            var isExistBot = checkExistBot(newBotAdd.Id);

            if (!isExistBot)
            {
                logService.LogInformation($"bot: {newBotAdd.Id} does not exist");
                return new CommandResult();
            }

            var result = await CreateAddBotToGroupCommand(message, newBotAdd);

            if (result.IsSuccessful && (bot == null || bot.Id == newBotAdd.Id))
            {
                string greeting = string.Format(CommandMessageTelegram.AddBotToGroupGreetingMessage, newBotAdd.Username, message.Chat.Id);
                await telegramBotService(newBotAdd.Id).SendTextMessageAsync(newBotAdd.Id, message.Chat.Id, greeting);
            }
            else
            {
                await logService.ErrorAsync(new Exception(result.Message));
            }

            logService.LogInformation("end AddBotToGroupAsync!!!");
            return result;
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
                UpdatedBy = message.From.Id.ToString()
            };
            var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);
            logService.LogInformation("start AddBotToGroupAsync: " + commandSerialize);

            var result = await baseService.ExecuteNonQueryAsync(command);
            return result;
        }

        protected internal virtual async Task RemoveBotToGroupAsync(Message message, User bot)
        {
            var isExistBot = checkExistBot(message.LeftChatMember.Id);
            if (isExistBot && message.LeftChatMember.IsBot)
            {
                var command = new RemoveBotFromGroupCommand
                {
                    BotId = message.LeftChatMember.Id,
                    ConversationId = message.Chat.Id.ToString(),
                    ChannelId = (byte)BotChannel.Telegram,
                    UpdatedBy = message.From.Id.ToString()
                };
                var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);
                logService.LogInformation("start RemoveBotToGroupAsync: " + commandSerialize);
                var result = await baseService.ExecuteNonQueryAsync(command);
                if (!result.IsSuccessful)
                {
                    await logService.ErrorAsync(new Exception(result.Message));
                }
                logService.LogInformation("end RemoveBotToGroupAsync!!!");
            }
        }

        protected internal virtual async Task<CommandResult> MigratedToSupergroupAsync(Message message)
        {
            var command = new UpdateConversationIdCommand
            {
                OldConversationId = message.Chat.Id.ToString(),
                NewConversationId = message.MigrateToChatId.ToString(),
                ChannelId = (byte)BotChannel.Telegram,
                UpdatedBy = message.From.Id.ToString()
            };
            var commandSerialize = JsonConvert.SerializeObject(command, Formatting.Indented);
            logService.LogInformation("start MigratedToSupergroupAsync: " + commandSerialize);
            var result = await baseService.ExecuteNonQueryAsync(command);
            if (!result.IsSuccessful)
            {
                await logService.ErrorAsync(new Exception(result.Message));
            }
            logService.LogInformation("end MigratedToSupergroupAsync!!!");
            return result;
        }

        protected internal virtual async Task AddBotToPersonalAsync(Message message, User bot)
        {
            logService.LogInformation("start AddBotToPersonal!!!");
            if (string.IsNullOrEmpty(message.Chat?.Username))
            {
                logService.LogInformation("empty username !!!");
                string errorMessageNotify = string.Format(CommandMessageTelegram.BotAddDoNotHaveUserName, bot.Username);
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
                string greeting = string.Format(CommandMessageTelegram.Wellcome, bot.Username, message.Chat.Id);
                await telegramBotService(bot.Id).SendTextMessageAsync(bot.Id, message.Chat.Id, greeting);
                if (!result.IsSuccessful)
                {
                    await logService.ErrorAsync(new Exception(result.Message));
                }
            }

            logService.LogInformation("end AddBotToPersonal!!!");
        }

        private bool checkExistBot(long botId) => botConfigurations.Exists(x => x.BotId == botId);
    }
}