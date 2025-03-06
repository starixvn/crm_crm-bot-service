using CRM.BotService.Criteria;
using CRM.BotService.Enum;
using CRM.BotService.Models;
using CRM.BotService.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CRM.BotService.Services;

public class BotCommonService(
    IBaseService baseService,
    IHttpClientFactory httpClientFactory,
    List<BotChannelInfo> botChannelInfos)
    : IBotCommonService
{
    public bool IsActiveChannel(BotChannel channel)
        => botChannelInfos is not null && botChannelInfos.Exists(c => c.Name == channel && c.IsActive);

    public async Task<byte[]> ReadImageAsByteAsync(string imgUrl)
    {
        var httpClient = httpClientFactory.CreateClient();
        return await httpClient.GetByteArrayAsync(new Uri(imgUrl));
    }

    public async Task<CommandResult> WriteLogMessageAsync(WriteLogMessageCommand command)
    {
        return await baseService.ExecuteNonQueryAsync(command);
    }
}