namespace CRM.BotService.Endpoint.UnitTests.TestableClasses.Telegram
{
    using Core.Models;
    using Core.Services;
    using Core.Services.Interfaces;
    using CRM.BotService.Endpoint.Controllers.Telegram;
    using global::Telegram.Bot.Types;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class UpdateControllerTestable : UpdateController
    {
        public UpdateControllerTestable(
            ILogServiceExtend logService,
            Func<long, ITelegramBotService> telegramBotService,
            IBaseService baseService,
            List<TelegramBotConfigurations> botConfigurations) : base(logService, telegramBotService, baseService, botConfigurations)
        {
        }

        public async Task Public_HandleEventUpdate(User bot, Message message)
        {
            await base.HandleEventUpdate(bot, message);
        }

        public async Task Public_HandleBotCommandAsync(User bot, Message message)
        {
            await base.HandleBotCommandAsync(bot, message);
        }

        public async Task<CommandResult> Public_EditChatTitledAsync(Message message)
        {
            return await base.EditChatTitledAsync(message);
        }

        public async Task<CommandResult> Public_AddBotToGroupAsync(Message message, User newBotAdd, User bot = null)
        {
            return await base.AddBotToGroupAsync(message, newBotAdd, bot);
        }

        public async Task Public_RemoveBotToGroupAsync(Message message, User bot)
        {
            await base.RemoveBotToGroupAsync(message, bot);
        }

        public async Task Public_AddBotToPersonalAsync(Message message, User bot)
        {
            await base.AddBotToPersonalAsync(message, bot);
        }

        public async Task<CommandResult> Public_MigratedToSupergroupAsync(Message message)
        {
            return await base.MigratedToSupergroupAsync(message);
        }
    }
}