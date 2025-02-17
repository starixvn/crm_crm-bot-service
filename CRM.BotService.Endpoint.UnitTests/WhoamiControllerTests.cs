namespace CRM.BotService.Endpoint.UnitTests
{
    using AutoFixture;
    using Controllers;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Net;
    using Xunit;

    public class WhoamiControllerTests
    {
        [Fact]
        public void Index_WhenCalled_ReturnExpectedContent()
        {
            var fixture = new Fixture();
            var controller = new WhoamiController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
            controller.ControllerContext.HttpContext.Connection.LocalIpAddress = new IPAddress(fixture.Create<long>());
            controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = new IPAddress(fixture.Create<long>());
            controller.ControllerContext.HttpContext.Request.Scheme = fixture.Create<string>();
            controller.ControllerContext.HttpContext.Request.Path = $"/{fixture.Create<string>()}";
            controller.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            var actual = controller.Whoami();
            Assert.IsType<ContentResult>(actual);
        }
    }
}