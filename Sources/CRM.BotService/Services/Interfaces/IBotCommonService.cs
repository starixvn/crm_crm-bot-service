using System.Threading.Tasks;
using CRM.BotService.Criteria;
using CRM.BotService.Enum;
using CRM.BotService.Models;

namespace CRM.BotService.Services.Interfaces;

public interface IBotCommonService
{
    Task<CommandResult> WriteLogMessageAsync(WriteLogMessageCommand command);

    bool IsActiveChannel(BotChannel channel);

    Task<byte[]> ReadImageAsByteAsync(string imgUrl);
}