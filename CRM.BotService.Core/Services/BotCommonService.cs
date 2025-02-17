namespace CRM.BotService.Core.Services
{
    using Criteria;
    using Enum;
    using Interfaces;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class BotCommonService : IBotCommonService
    {
        private readonly IBaseService baseService;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly List<BotChannelInfo> botChannelInfos;

        public BotCommonService(IBaseService baseService, IHttpClientFactory httpClientFactory, List<BotChannelInfo> botChannelInfos)
        {
            this.baseService = baseService;
            this.botChannelInfos = botChannelInfos;
            this.httpClientFactory = httpClientFactory;
        }

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
}