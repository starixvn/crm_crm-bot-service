namespace CRM.BotService.Endpoint.Controllers;

using Microsoft.AspNetCore.Mvc;
using Services;
using System.Threading.Tasks;

public class HomeController(ILogServiceExtend logServiceExtend) : Controller
{
    public IActionResult Index()
    {
        return Ok("OK");
    }

    [Route("api/sentry")]
    public async Task<IActionResult> TestLogSentry()
    {
        await logServiceExtend.ErrorMessageAsync("Test log sentry");
        return Ok("Sentry test");
    }
}