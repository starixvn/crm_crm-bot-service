namespace CRM.BotService.Core.Services.Interfaces
{
    using Criteria;
    using Enum;
    using Models;
    using System.Threading.Tasks;

    public interface IBotCommonService
    {
        Task<CommandResult> WriteLogMessageAsync(WriteLogMessageCommand command);

        bool IsActiveChannel(BotChannel channel);

        Task<byte[]> ReadImageAsByteAsync(string imgUrl);
    }
}