using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class HttpClientExtensionsTests
    {
        private static ISetup<HttpMessageHandler, Task<HttpResponseMessage>> SetupSendAsync(Mock<HttpMessageHandler> mockHttpMessageHandler)
        {
            return mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetAsync()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("It worked!") };
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            SetupSendAsync(mockHttpMessageHandler).ReturnsAsync(() => response);
            var httpClient = new PlayerHttpClient(new HttpClient(mockHttpMessageHandler.Object));

            var getResult = await httpClient.GetAsync("https://anywhere.com", TimeSpan.FromMinutes(5));
            Assert.Equal(HttpStatusCode.OK, getResult.StatusCode);
        }

        [Fact]
        public async Task SendAsync()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("It worked!") };
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            SetupSendAsync(mockHttpMessageHandler).ReturnsAsync(() => response);
            var httpClient = new PlayerHttpClient(new HttpClient(mockHttpMessageHandler.Object));

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://someapi.com", UriKind.Absolute),
                Method = HttpMethod.Get
            };
            var getResult = await httpClient.SendAsync(request, TimeSpan.FromMinutes(5));
            Assert.Equal(HttpStatusCode.OK, getResult.StatusCode);
        }

        [Fact]
        public async Task GetAsyncCancel()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("It worked!") };
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            SetupSendAsync(mockHttpMessageHandler).ThrowsAsync(new OperationCanceledException());
            var httpClient = new PlayerHttpClient(new HttpClient(mockHttpMessageHandler.Object));

            await Assert.ThrowsAsync<TimeoutException>(async () => await httpClient.GetAsync("https://anywhere.com", TimeSpan.FromMinutes(5)));
        }

        [Fact]
        public async Task GetUnknownHost()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("It worked!") };
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            SetupSendAsync(mockHttpMessageHandler).ThrowsAsync(new HttpRequestException());
            var httpClient = new PlayerHttpClient(new HttpClient(mockHttpMessageHandler.Object));

            await Assert.ThrowsAsync<PlayerCommunicationException>(async () => await httpClient.GetAsync("https://anywhere.com", TimeSpan.FromMinutes(5)));
        }

        [Fact]
        public async Task GetWrongStatusCode()
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Error") };
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            SetupSendAsync(mockHttpMessageHandler).ReturnsAsync(response);
            var httpClient = new PlayerHttpClient(new HttpClient(mockHttpMessageHandler.Object));

            var ex = await Assert.ThrowsAsync<InvalidStatusCodeException>(async () => await httpClient.GetAsync("https://anywhere.com", TimeSpan.FromMinutes(5)));
            Assert.Equal("Error", ex.Data[nameof(InvalidStatusCodeException.Content)]);
        }
    }
}
