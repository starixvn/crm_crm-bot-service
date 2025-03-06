using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.BotService.Tests.Helpers;

internal class MockHttpMessageHandler(HttpResponseMessage responseMsg) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(responseMsg);
    }
}

internal class MockHttpMessageHandlerMultiple(IDictionary<string, HttpResponseMessage> messages) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        if (request.RequestUri != null && messages.ContainsKey(request.RequestUri.ToString()))
            response = messages[request.RequestUri.ToString()] ?? new HttpResponseMessage(HttpStatusCode.NoContent);
        response.RequestMessage = request;
        return Task.FromResult(response);
    }
}