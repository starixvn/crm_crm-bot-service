namespace CRM.BotService.Endpoint.Controllers
{
    using Core.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    public class HomeController(ILogServiceExtend logServiceExtend) : Controller
    {
        public IActionResult Index()
        {
            return Content("Bot Service running");
        }

        [Route("api/sentry")]
        public async Task<IActionResult> TestLogSentry()
        {
            await logServiceExtend.ErrorMessageAsync("Test log sentry");
            return Ok("Sentry test");
        }
    }
}