using CRM.BotService.Criteria;
using CRM.BotService.Enum;
using CRM.BotService.Models;
using CRM.BotService.Services;
using NSubstitute;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CRM.BotService.Tests.Services;

public class BotCommonServiceTests
{
    private IBaseService baseService;
    private IHttpClientFactory httpClientFactory;

    [Fact]
    public void IsActiveChannel_WhenCalled_ReturnExpectedResult()
    {
        var service = GetService();
        var actual = service.IsActiveChannel(BotChannel.Telegram);
        Assert.False(actual);

        var actual2 = service.IsActiveChannel(BotChannel.Letstalk);
        Assert.True(actual2);
    }

    [Fact]
    public async Task WriteLogMessageAsync_WhenCalled_CallBaseServiceAndReturnExpectedResult()
    {
        var service = GetService();
        var expected = new CommandResult { Code = 200, Message = "Error" };
        baseService.ExecuteNonQueryAsync(Arg.Any<WriteLogMessageCommand>()).Returns(expected);

        var actual = await service.WriteLogMessageAsync(new());

        Assert.False(actual.IsSuccessful);
        Assert.Equal(200, actual.Code);
        Assert.Equal("Error", actual.Message);
    }

    [Fact]
    public async Task ReadImageAsByteAsync_WhenCalled_CallHttpClient_ReturnExpectedByteArray()
    {
        var service = GetService();

        var actual = await service.ReadImageAsByteAsync("http://tets.com/1.png");

        Assert.Equal(4, actual.Length);
    }

    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private BotCommonService GetService()
    {
        baseService = Substitute.For<IBaseService>();
        httpClientFactory = Substitute.For<IHttpClientFactory>();
        List<BotChannelInfo> channels =
        [
            new() { Name = BotChannel.Telegram, IsActive = false },
            new() { Name = BotChannel.Letstalk, IsActive = true }
        ];
        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new FakeHttpContent("Test")
        };
        httpClientFactory.CreateClient().Returns(new HttpClient(new MockHttpMessageHandler(fakeResponse)));
        return new(baseService, httpClientFactory, channels);
    }
}

internal class MockHttpMessageHandler(HttpResponseMessage messages) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var responseTask = new TaskCompletionSource<HttpResponseMessage>();
        responseTask.SetResult(messages);

        return responseTask.Task;
    }
}

internal class FakeHttpContent(string content) : HttpContent
{
    private string Content { get; } = content;

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        var byteArray = Encoding.ASCII.GetBytes(Content);
        await stream.WriteAsync(byteArray, 0, Content.Length);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = Content.Length;
        return true;
    }
}