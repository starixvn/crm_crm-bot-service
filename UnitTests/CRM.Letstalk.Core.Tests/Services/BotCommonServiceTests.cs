using CRM.BotService.Core.Enum;

namespace CRM.BotService.Core.Tests.Services
{
    using CRM.BotService.Core.Criteria;
    using CRM.BotService.Core.Services;
    using Models;
    using NSubstitute;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

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

            var actual = await service.WriteLogMessageAsync(new WriteLogMessageCommand());

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
            List<BotChannelInfo> channels = new()
            {
                new BotChannelInfo { Name = BotChannel.Telegram, IsActive = false},
                new BotChannelInfo { Name = BotChannel.Letstalk, IsActive = true},
            };
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new FakeHttpContent("Test")
            };
            httpClientFactory.CreateClient().Returns(new HttpClient(new MockHttpMessageHandler(fakeResponse)));
            return new BotCommonService(baseService, httpClientFactory, channels);
        }
    }

    internal class MockHttpMessageHandler : DelegatingHandler
    {
        private readonly HttpResponseMessage messages;

        public MockHttpMessageHandler(HttpResponseMessage messages)
        {
            this.messages = messages;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var responseTask = new TaskCompletionSource<HttpResponseMessage>();
            responseTask.SetResult(messages);

            return responseTask.Task;
        }
    }

    internal class FakeHttpContent : HttpContent
    {
        public string Content { get; set; }

        public FakeHttpContent(string content)
        {
            Content = content;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(Content);
            await stream.WriteAsync(byteArray, 0, Content.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = Content.Length;
            return true;
        }
    }
}