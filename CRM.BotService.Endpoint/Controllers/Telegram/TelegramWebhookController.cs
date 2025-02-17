namespace CRM.BotService.Endpoint.Controllers.Telegram
{
    using Core.Models;
    using Core.Services.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class TelegramWebhookController : Controller
    {
        private readonly List<TelegramBotConfigurations> botConfigurations;
        private readonly Func<long, ITelegramBotService> telegramBotService;

        public TelegramWebhookController(
            List<TelegramBotConfigurations> botConfigurations,
            Func<long, ITelegramBotService> telegramBotService)
        {
            this.botConfigurations = botConfigurations;
            this.telegramBotService = telegramBotService;
        }

        [AllowAnonymous]
        [Route("api/telegram/bot/setWebhook")]
        public async Task<IActionResult> SetWebhook()
        {
            try
            {
                foreach (var bot in botConfigurations)
                {
                    await telegramBotService(bot.BotId).SetWebhookAsync(bot);
                }
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }

            return Ok("Successfully!");
        }

        [AllowAnonymous]
        [Route("api/telegram/bot/getWebhookInfo")]
        public async Task<IActionResult> GetWebhookInfo(long botId)
        {
            var bot = botConfigurations.Find(config => config.BotId == botId);
            if (bot is not null)
            {
                var webhookInfo = await telegramBotService(bot.BotId).GetWebhookInfoAsync(bot);
                return Ok(webhookInfo);
            }
            else
            {
                return Ok("bot does not exist.");
            }
        }

        [AllowAnonymous]
        [Route("api/telegram/bot/healthCheck")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var botIds = botConfigurations.Select(x => x.BotId);
                foreach (var botId in botIds)
                {
                    var user = await telegramBotService(botId).GetBotInfoAsync(botId);
                    if (user is null)
                    {
                        return Ok($"bot {botId} does not exist.");
                    }
                }
                return Ok("bots are healthy!!!");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }
    }
}