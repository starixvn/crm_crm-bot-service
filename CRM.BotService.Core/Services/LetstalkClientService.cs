namespace CRM.BotService.Core.Services
{
    using Constants;
    using CRM.BotService.Core.Enum;
    using Extensions.Letstalk;
    using Fanex.Caching;
    using Interfaces;
    using Models.Letstalk;
    using Models.Letstalk.BotConfig;
    using Models.Letstalk.Results;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class LetstalkClientService : ILetstalkClientService
    {
        private const string LetstalkCacheKey = "CRM-Letstalk-API-Token";
        private const string JsonContentType = "application/json";
        private const string PlainTextContentType = "text/plain";
        private const int ExpiredTokenPeriodInMiliseconds = 5000;
        private readonly LetstalkBotConfig botConfig;
        private readonly IHttpClientFactory clientFactory;
        private readonly ICacheService cacheService;

        public LetstalkClientService(IHttpClientFactory clientFactory,
            ICacheService cacheService,
            LetstalkBotConfig botConfig)
        {
            this.cacheService = cacheService;
            this.clientFactory = clientFactory;
            this.botConfig = botConfig;
        }

        public async Task<BaseResult> ChangeDisplayNameAsync(string botName)
        {
            ValidateInputParam(nameof(botName), botName);

            return await ExecutePutAsync<BaseResult>(
                GetActualEndpoint($"bot/change_display_name/{botConfig.BotAccountId}"),
                PlainTextContentType,
                Encode(new Dictionary<string, object>
                {
                    { "display_name", botName }
                }),
                Base64ApiKeyEncoded()
                );
        }

        public async Task<IList<string>> GetBotContactList()
        {
            var result = await ExecuteGetAsync<BotContactListResult>(
                GetActualEndpoint($"bot/v2/contact?botId={botConfig.BotAccountId}"),
                PlainTextContentType,
                token: Base64ApiKeyEncoded());
            if (result.IsSuccess)
            {
                return result.Contacts;
            }

            return new List<string>();
        }

        public async Task<RoomData> GetChatRoomData(string roomToken)
        {
            ValidateInputParam(nameof(roomToken), roomToken);

            var result = await ExecuteGetAsync<RoomResult>(
                GetActualEndpoint($"bot/v2/chatroom/data?botId={botConfig.BotAccountId}&room={roomToken}"),
                PlainTextContentType,
                token: Base64ApiKeyEncoded());

            if (result.IsSuccess)
            {
                return result.Room;
            }

            return null;
        }

        public async Task<Contact> GetProfileAsync(string accountId)
        {
            ValidateInputParam(nameof(accountId), accountId);

            var result = await ExecuteGetAsync<ProfileResult>(GetActualEndpoint($"bot/get_profile/{botConfig.BotAccountId}/user/{accountId}"), PlainTextContentType, token: Base64ApiKeyEncoded());

            if (result != null && result.IsSuccess)
            {
                return new Contact
                {
                    UserId = accountId,
                    AvatarUrl = result.AvatarUrl,
                    DisplayName = result.DisplayName
                };
            }

            return null;
        }

        public async Task<PushMessageResult> PushRoomAsync(string roomId, OutgoingMessage message)
            => await PushRoomAsync(new List<string> { roomId }, message);

        public async Task<PushMessageResult> PushRoomAsync(IEnumerable<string> roomList, OutgoingMessage message)
        {
            ValidateInputParam(nameof(roomList), roomList);
            ValidateInputParam(nameof(message), message);
            ValidateMultiCastMessage(message);

            var token = await GetSignAccessTokenAsync();
            var result = await ExecutePostAsync<PushMessageResult>(
                GetActualEndpoint($"bot/v3/message/multi-chatroom"),
                JsonContentType,
                JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "msg", message },
                    { "receivers",  roomList.ToArray() }
                }),
                token);

            return result;
        }

        private static void ValidateMultiCastMessage(OutgoingMessage message)
        {
            if ((message.MessageType == NotificationMessageType.Html || message.MessageType == NotificationMessageType.PlainText)
                && string.IsNullOrWhiteSpace(message.Text))
            {
                throw new ArgumentNullException(message.Text);
            }

            if ((message.MessageType == NotificationMessageType.Image || message.MessageType == NotificationMessageType.FullImage)
                && string.IsNullOrWhiteSpace(message.ImageUrl))
            {
                throw new ArgumentNullException(message.ImageUrl);
            }
        }

        private async Task<string> GetSignAccessTokenAsync()
        {
            var token = await cacheService.GetAsync<string>(LetstalkCacheKey);

            if (string.IsNullOrWhiteSpace(token))
            {
                var result = await ExecutePostAsync<TokenResult>(
                      GetActualEndpoint($"bot/v3/sign-token"),
                      JsonContentType,
                      JsonConvert.SerializeObject(
                      new Dictionary<string, object>
                      {
                            { "id", botConfig.BotAccountId },
                            { "token", botConfig.ApiKey }
                      }));

                token = result.Token;
                await cacheService.SetAsync(
                    LetstalkCacheKey,
                    token,
                    new CacheItemOptions().SetAbsoluteExpiration(TimeSpan.FromDays(5)));
            }

            return token;
        }

        public async Task<BaseResult> SetWebhookAsync(string url)
        {
            ValidateInputParam(nameof(url), url);
            return await ExecutePostAsync<BaseResult>(
                GetActualEndpoint($"bot/set_webhook/{botConfig.BotAccountId}"),
                PlainTextContentType,
                Encode(
                new Dictionary<string, object>
                {
                    { "url", url }
                }),
                Base64ApiKeyEncoded());
        }

        private static void ValidateInputParam(string paramName, object paramValue)
        {
            if (paramValue == null
                || (paramValue is string && string.IsNullOrWhiteSpace(paramValue as string)))
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public async Task<IList<string>> GetChatRoomList()
        {
            var result = await ExecuteGetAsync<BotChatRoomListResult>(
                    GetActualEndpoint($"bot/v2/chatroom?botId={botConfig.BotAccountId}"),
                    PlainTextContentType,
                    token: Base64ApiKeyEncoded()
            );
            if (result.IsSuccess)
            {
                return result.Rooms;
            }

            return new List<string>();
        }

        public async Task<string> GetWebhookAsync()
        {
            var result = await ExecuteGetAsync<WebhookResult>(
                GetActualEndpoint($"bot/get_webhook/{botConfig.BotAccountId}"), PlainTextContentType, token: Base64ApiKeyEncoded());

            if (result.IsSuccess)
            {
                return result.Url;
            }

            return string.Empty;
        }

        public async Task<PushMessageResult> PushPersonalAsync(string letstalkId, OutgoingMessage message, bool isForcePush = true)
            => await PushPersonalAsync(new List<string> { letstalkId }, message, isForcePush);

        public async Task<PushMessageResult> PushPersonalAsync(IEnumerable<string> accountIds, OutgoingMessage message, bool isForcePush = true)
        {
            ValidateInputParam(nameof(accountIds), accountIds);
            ValidateInputParam(nameof(message), message);
            ValidateMultiCastMessage(message);

            var token = await GetSignAccessTokenAsync();

            var result = await ExecutePostAsync<PushMessageResult>(
                GetActualEndpoint($"bot/v3/message/multicast"),
                JsonContentType,
                JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { "msg", message },
                    { "receivers",accountIds.ToArray() },
                    { "forcePush", isForcePush ? 1 : 0 }
                }),
                token
                );

            return result;
        }

        private string Encode(IDictionary<string, object> data)
        {
            var totalMilisecondNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            data.Add("iat", totalMilisecondNow);
            data.Add("exp", totalMilisecondNow + ExpiredTokenPeriodInMiliseconds);

            var result = JwtHelper.Encode(botConfig.SecureKey, data);
            return result;
        }

        private string Base64ApiKeyEncoded()
            => Convert.ToBase64String(Encoding.UTF8.GetBytes(botConfig.ApiKey));

        private string GetActualEndpoint(string endpoint) => botConfig.ApiEndpoint + endpoint;

        #region HttpClient

        private Task<T> ExecuteGetAsync<T>(string endPoint, string contentType, string inputParams = null, string token = "") where T : new()
            => SendHttpClientAsync<T>(endPoint, HttpMethod.Get, inputParams, token, contentType);

        private Task<T> ExecutePostAsync<T>(string endPoint, string contentType, string inputParams, string token = "") where T : new()
            => SendHttpClientAsync<T>(endPoint, HttpMethod.Post, inputParams, token, contentType);

        private Task<T> ExecutePutAsync<T>(string endPoint, string contentType, string inputParams, string token = "") where T : new()
            => SendHttpClientAsync<T>(endPoint, HttpMethod.Put, inputParams, token, contentType);

        private async Task<T> SendHttpClientAsync<T>(string endPoint, HttpMethod method, string inputParams, string token, string contentType)
            where T : new()
        {
            ValidateEndPoint(endPoint);

            using var request = CreateRequest(endPoint, method, token);
            if (inputParams != null)
            {
                request.Content = new StringContent(inputParams, Encoding.UTF8, contentType);
            }
            var client = clientFactory.CreateClient(NamedHttpClients.ProxiedClient);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await cacheService.RemoveAsync(LetstalkCacheKey);
            }
            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(responseData);

            return result;
        }

        private static HttpRequestMessage CreateRequest(string endPoint, HttpMethod method, string token)
        {
            Uri url = new(endPoint);

            var request = new HttpRequestMessage(method, url);
            request.Headers.Accept.Clear();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return request;
        }

        private static void ValidateEndPoint(string endPoint)
        {
            if (string.IsNullOrWhiteSpace(endPoint))
            {
                throw new ArgumentNullException(nameof(endPoint));
            }
        }

        #endregion HttpClient
    }
}