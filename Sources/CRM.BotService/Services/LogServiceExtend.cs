using CRM.BotService.Models;
using Fanex.Data.Repository;
using Fanex.Logging.Extensions;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using System;
using System.Threading.Tasks;

namespace CRM.BotService.Services;

public interface ILogServiceExtend
{
    Task ErrorMessageAsync(string error);

    Task InfoMessageAsync(string info);

    Task ErrorAsync(CommandResult commandResult, ICriteria criteria);

    Task ErrorAsync(Exception exception, ICriteria criteria);

    Task ErrorAsync(Exception exception);

    void LogInformation(string message);

    void LogFileError(Exception exception);
}

public class LogServiceExtend(Fanex.Logging.ILogger logger) : ILogServiceExtend
{
    private readonly Logger nlog = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

    public async Task ErrorMessageAsync(string error)
    {
        await logger.ErrorAsync(error);
    }

    public async Task InfoMessageAsync(string info)
    {
        await logger.InfoAsync(info);
    }

    public async Task ErrorAsync(Exception exception)
    {
        var logMessage = new LogMessage(exception.Message)
            .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
            .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
            .ToString();
        await logger.ErrorAsync(logMessage);
    }

    public async Task ErrorAsync(CommandResult commandResult, ICriteria criteria)
    {
        var logMessage = new LogMessage($"{commandResult.Code}: {commandResult.Message}")
            .AddMessage(JsonConvert.SerializeObject(criteria, Formatting.Indented))
            .ToString();
        await logger.ErrorAsync(logMessage);
    }

    public async Task ErrorAsync(Exception exception, ICriteria criteria)
    {
        var logMessage = new LogMessage(exception.Message)
            .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
            .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
            .AddMessage(JsonConvert.SerializeObject(criteria, Formatting.Indented))
            .ToString();
        await logger.ErrorAsync(logMessage);
    }

    public void LogFileError(Exception exception)
    {
        nlog.Error(exception);
    }

    public void LogInformation(string message)
    {
        nlog.Info(message);
    }
}