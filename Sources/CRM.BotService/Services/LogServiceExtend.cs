using Fanex.Logging.Extensions;
using NLog;
using NLog.Web;
using System;
using System.Threading.Tasks;

namespace CRM.BotService.Services;

public interface ILogServiceExtend
{
    Task ErrorMessageAsync(string error);

    Task ErrorAsync(Exception exception);

    void LogInformation(string message);
}

public class LogServiceExtend(Fanex.Logging.ILogger logger) : ILogServiceExtend
{
    private readonly Logger nlog = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

    public async Task ErrorMessageAsync(string error)
    {
        await logger.ErrorAsync(error);
    }

    public async Task ErrorAsync(Exception exception)
    {
        var logMessage = new LogMessage(exception.Message)
            .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
            .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception))
            .ToString();
        await logger.ErrorAsync(logMessage);
    }

    public void LogInformation(string message)
    {
        nlog.Info(message);
    }
}