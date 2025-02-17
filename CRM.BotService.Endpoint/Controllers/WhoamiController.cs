namespace CRM.BotService.Endpoint.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Text;

    public class WhoamiController : Controller
    {
        [Route("Whoami")]
        [Route("Whoami.aspx")]
        [AllowAnonymous]
        public IActionResult Whoami()
        {
            var content = new StringBuilder();
            content.Append($"Web name: {HttpContext.Request.Host.Host}{Environment.NewLine}"
                           + $"Port: {HttpContext.Connection.LocalPort}{Environment.NewLine}"
                           + $"Server IP: {HttpContext.Connection.LocalIpAddress}{Environment.NewLine}"
                           + $"Server Time: {DateTimeOffset.Now.DateTime}{Environment.NewLine}"
                           + $"Client IP: {HttpContext.Connection.RemoteIpAddress}{Environment.NewLine}"
                           + $"Request Scheme: {HttpContext.Request.Scheme}{Environment.NewLine}"
                           + $"Request Path: {HttpContext.Request.Path}{Environment.NewLine}{Environment.NewLine}"
                           + $"Request Headers:{Environment.NewLine}");

            foreach (var header in HttpContext.Request.Headers)
            {
                content.Append($"- {header.Key}: " +
                               $"{header.Value}{Environment.NewLine}");
            }

            return Content(content.ToString());
        }
    }
}