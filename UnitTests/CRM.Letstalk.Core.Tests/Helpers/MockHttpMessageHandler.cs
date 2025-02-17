namespace CRM.BotService.Core.Tests.Helpers
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    internal class MockHttpMessageHandler : DelegatingHandler
    {
        private readonly HttpResponseMessage responseMsg;

        public MockHttpMessageHandler(HttpResponseMessage responseMsg)
        {
            this.responseMsg = responseMsg;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseMsg);
        }
    }

    internal class MockHttpMessageHandlerMultiple : DelegatingHandler
    {
        private readonly IDictionary<string, HttpResponseMessage> messages;

        public MockHttpMessageHandlerMultiple(IDictionary<string, HttpResponseMessage> messages)
        {
            this.messages = messages;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            if (messages.ContainsKey(request.RequestUri.ToString()))
                response = messages[request.RequestUri.ToString()] ?? new HttpResponseMessage(HttpStatusCode.NoContent);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}