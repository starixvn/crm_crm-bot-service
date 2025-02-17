namespace CRM.BotService.Services.Telegram.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using CRM.BotService.Services.Letstalk.Services.Interfaces;
    using CRM.BotService.Services.Telegram.Models.Result;
    using CRM.BotService.Services.Telegram.Services.Interfaces;
    using CRM.Letstalk.Core.Models;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    public class TelegramClientService : ITelegramClientService
    {
        private readonly IHttpClientService httpClientService;
        private readonly TelegramAppSettings telegramAppSettings;

        private readonly string contentType = "application/json";

        public TelegramClientService(IHttpClientService httpClientService, IOptions<TelegramAppSettings> telegramAppSettings)
        {
            this.httpClientService = httpClientService;
            this.telegramAppSettings = telegramAppSettings.Value;
        }

        public async Task<PushMessageTelegramResult> PushPersonalByTelegramAsync(long botId, long chatId, string message)
        {
            try
            {
                ValidateInputParam(nameof(chatId), chatId);
                ValidateInputParam(nameof(message), message);
                var endpoint = $"{telegramAppSettings.Domain}/{telegramAppSettings.ApiConfigs[0].SendMessageUrl}";
                var jwt = await GetTokenAsync();
                var result = await httpClientService.ExecutePostAsync<PushMessageTelegramResult>(
                    endpoint,
                    contentType,
                    JsonConvert.SerializeObject(
                        new Dictionary<string, object>
                        {
                        { "BotConversationId", botId },
                        { "ConversationId", chatId},
                        { "Message", message},
                        }),
                    jwt.Token);

                return result;
            }
            catch (Exception e)
            {

                throw;
            }
        }

        protected internal virtual async Task<JwtToken> GetTokenAsync()
        {
            var url = $"{telegramAppSettings.Domain}/{telegramAppSettings.AuthenticateUrl}";
            var endpoint = $"{url}?username={telegramAppSettings.AppCredentialId}&password={telegramAppSettings.AppCredentialPassword}";
            var jwtToken = await httpClientService.ExecuteGetAsync<JwtToken>(endpoint, contentType);
            return jwtToken;
        }

        private void ValidateInputParam(string paramName, object paramValue)
        {
            if (paramValue == null
                || (paramValue is string && string.IsNullOrWhiteSpace(paramValue as string)))
            {
                throw new ArgumentNullException(paramName);
            }
        }

    }
}
