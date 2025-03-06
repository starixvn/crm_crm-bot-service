using CRM.BotService.Models;
using Fanex.Data.Repository;
using Fanex.NAP.Client.Helpers.Extensions;
using System.Threading.Tasks;

namespace CRM.BotService.Services;

public interface IBaseService
{
    Task<CommandResult> ExecuteNonQueryAsync(INonQueryCommand command);
}

public class BaseService(IDynamicRepository dynamicRepository) : IBaseService
{
    public async Task<CommandResult> ExecuteNonQueryAsync(INonQueryCommand command)
    {
        var commandResult = new CommandResult();
        if (command == null || !command.IsValid())
        {
            return commandResult;
        }

        await dynamicRepository.ExecuteAsync(command);

        var properties = command.ToPropertyDictionary();
        if (!properties.TryGetValue("ErrCode", out var property))
        {
            return commandResult;
        }

        commandResult.Code = int.Parse(property.ToString() ?? "1");
        commandResult.Message = properties["ErrMsg"] != null ? properties["ErrMsg"].ToString() : string.Empty;

        return commandResult;
    }
}