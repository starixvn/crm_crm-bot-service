using CRM.BotService.Models.Letstalk;
using CRM.BotService.Models.Letstalk.Result;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CRM.BotService.Services.Interfaces;

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

    Task<IList<string>> GetBotContactList();

    Task<IList<string>> GetChatRoomList();
}