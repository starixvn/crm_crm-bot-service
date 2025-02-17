namespace CRM.BotService.Core.Services.Interfaces
{
    using Models.Letstalk;
    using Models.Letstalk.Results;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ILetstalkClientService
    {
        Task<PushMessageResult> PushPersonalAsync(string letstalkId, OutgoingMessage message, bool isForcePush = true);

        Task<PushMessageResult> PushPersonalAsync(IEnumerable<string> accountIds, OutgoingMessage message, bool isForcePush = true);

        Task<PushMessageResult> PushRoomAsync(string roomId, OutgoingMessage message);

        Task<PushMessageResult> PushRoomAsync(IEnumerable<string> roomList, OutgoingMessage message);

        Task<Contact> GetProfileAsync(string accountId);

        Task<RoomData> GetChatRoomData(string roomToken);

        Task<string> GetWebhookAsync();

        Task<BaseResult> SetWebhookAsync(string url);

        Task<BaseResult> ChangeDisplayNameAsync(string botName);

        Task<IList<string>> GetBotContactList();

        Task<IList<string>> GetChatRoomList();
    }
}