namespace CRM.BotService.Endpoint.Controllers;

using EventHandler;
using Microsoft.AspNetCore.Mvc;
using Models;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LetstalkWebhookController(ILetstalkEventHandler eventHandler,
    Func<long, ILetstalkClientService> letstalkService,
    List<WebhookConfiguration> webhookConfigurations
) : ControllerBase
{
    [Route("api/webhook/{id}")]
    public async Task WebhookReceiver(int id)
    {
        await eventHandler.ReceiveAsync(Request, id);
    }

    [Route("api/setwebhook/{id}")]
    public async Task<IActionResult> SetWebhook(int id)
    {
        var botConfigs = webhookConfigurations.Find(config => config.BotId == id);
        if (botConfigs is { IsAllowedSet: true })
        {
            var webhookUrl = $"{botConfigs.WebhookBaseUrl}api/webhook/{id}";
            var result = await letstalkService(id).SetWebhookAsync(webhookUrl);
            return Ok(result);
        }

        return NotFound();
    }

    [Route("api/getwebhook/{id}")]
    public async Task<IActionResult> GetWebhook(int id)
    {
        var botConfigs = webhookConfigurations.Find(config => config.BotId == id);
        if (botConfigs is { IsAllowedGet: true })
        {
            var result = await letstalkService(id).GetWebhookAsync();
            return Ok(result);
        }

        return NotFound();
    }
}