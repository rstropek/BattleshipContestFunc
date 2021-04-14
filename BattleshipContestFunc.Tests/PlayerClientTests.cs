using Microsoft.Extensions.Configuration;
using Moq;
using NBattleshipCodingContest.Logic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class PlayerClientTests
    {
        [Fact]
        public void BuildPathWithoutKey()
        {
            Assert.Equal("a/b", PlayerClient.BuildPathWithKey("a/b", null));
        }

        [Fact]
        public void BuildPathWithKey()
        {
            Assert.Equal("a/b?code=key", PlayerClient.BuildPathWithKey("a/b", "key"));
        }

        [Fact]
        public void BuildPathWithKeyAndParameters()
        {
            Assert.Equal("a/b?code=key&foo=bar", PlayerClient.BuildPathWithKey("a/b", "key", new KeyValuePair<string, string>[] { new("foo", "bar") }));
        }

        [Fact]
        public void BuildPathWithoutKeyAndParameters()
        {
            Assert.Equal("a/b?foo=bar", PlayerClient.BuildPathWithKey("a/b", null, new KeyValuePair<string, string>[] { new("foo", "bar") }));
        }

        [Fact]
        public void BuildPathWithKeyAndMultipleParameters()
        {
            Assert.Equal("a/b?code=key&foo=bar&x=y", PlayerClient.BuildPathWithKey("a/b", "key", 
                new KeyValuePair<string, string>[] { new("foo", "bar"), new("x", "y") }));
        }

        [Fact]
        public async Task GetReady()
        {
            var clientMock = new Mock<IPlayerHttpClient>();
            clientMock.Setup(m => m.GetAsync("getReady?code=key&numberOfGames=42", TimeSpan.FromMilliseconds(2345)));

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(m => m["Timeouts:getReady"]).Returns("2345");
            configMock.Setup(m => m["Timeouts:getShot"]).Returns("1234");
            configMock.Setup(m => m["Timeouts:getShots"]).Returns("3456");
            configMock.Setup(m => m["Timeouts:finished"]).Returns("3456");

            var factoryMock = new Mock<IPlayerHttpClientFactory>();
            factoryMock.Setup(m => m.GetHttpClient("https://someApi.com")).Returns(clientMock.Object);

            var client = new PlayerClient(factoryMock.Object, configMock.Object);
            await client.GetReady("https://someApi.com", 42, "key");

            factoryMock.VerifyAll();
        }

        [Fact]
        public async Task GetShot()
        {
            var clientMock = new Mock<IPlayerHttpClient>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("\"A1\"", Encoding.UTF8, "application/json")
            };

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(m => m["Timeouts:getReady"]).Returns("2345");
            configMock.Setup(m => m["Timeouts:getShot"]).Returns("1234");
            configMock.Setup(m => m["Timeouts:getShots"]).Returns("3456");
            configMock.Setup(m => m["Timeouts:finished"]).Returns("3456");

            Expression<Func<HttpRequestMessage, bool>> check = message =>
                message.RequestUri!.ToString() == "getShot?code=key"
                && message.Method == HttpMethod.Post
                && message.Content != null;
            clientMock.Setup(m => m.SendAsync(It.Is(check), TimeSpan.FromMilliseconds(1234)))
                .Callback<HttpRequestMessage, TimeSpan>((msg, to) =>
                {
                    var shotRequest = msg.Content!.ReadFromJsonAsync<ShotRequest>().Result;
                    Assert.NotNull(shotRequest);
                    Assert.Equal("A1", shotRequest!.LastShot);
                    Assert.NotNull(shotRequest!.Board);
                })
                .ReturnsAsync(response);

            var factoryMock = new Mock<IPlayerHttpClientFactory>();
            factoryMock.Setup(m => m.GetHttpClient("https://someApi.com")).Returns(clientMock.Object);

            var gameMock = new Mock<ISinglePlayerGame>();
            gameMock.SetupGet(m => m.Log).Returns(new List<SinglePlayerGameLogRecord>());
            gameMock.SetupGet(m => m.LastShot).Returns(new BoardIndex());
            gameMock.SetupGet(m => m.ShootingBoard).Returns(new BoardContent());

            var client = new PlayerClient(factoryMock.Object, configMock.Object);
            var shot = await client.GetShot("https://someApi.com", gameMock.Object, "key");

            factoryMock.VerifyAll();
            Assert.Equal("A1", shot);
        }
    }
}
