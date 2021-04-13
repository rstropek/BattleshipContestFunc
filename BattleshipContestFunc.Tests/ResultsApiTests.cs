using BattleshipContestFunc.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class ResultsApiTests : IClassFixture<ApiConfigFixture>
    {
        private readonly ApiConfigFixture config;

        public ResultsApiTests(ApiConfigFixture config)
        {
            this.config = config;
        }

        [Fact]
        public async Task Get()
        {
            var resultTable = new Mock<IPlayerResultTable>();
            var results = new List<PlayerResult>()
            { 
                new(Guid.NewGuid()) { AvgNumberOfShots = 40, Name = "Foo" },
                new(Guid.NewGuid()) { AvgNumberOfShots = 20, Name = "Bar" },
            };
            resultTable.Setup(m => m.Get(null)).ReturnsAsync(results);

            var userId = Guid.NewGuid();
            var playerTable = new Mock<IPlayerTable>();
            playerTable.Setup(m => m.GetSingle(Guid.Parse(results[0].RowKey)))
                .ReturnsAsync(new Player(Guid.Parse(results[0].RowKey))
                {
                    PartitionKey = userId.ToString(),
                    Name = "Foo", 
                    GitHubUrl = "https://github.com/fooplayer",
                    Creator = "foobar"
                });
            playerTable.Setup(m => m.GetSingle(Guid.Parse(results[1].RowKey)))
                .ReturnsAsync(new Player(Guid.Parse(results[1].RowKey))
                {
                    PartitionKey = userId.ToString(),
                    Name = "Bar", 
                    GitHubUrl = "https://github.com/barplayer",
                    Creator = "foobar"
                });

            var userTable = new Mock<IUsersTable>();
            userTable.Setup(m => m.GetSingle("foobar"))
                .ReturnsAsync(new User("foobar") { PublicTwitter = "@foobar" });

            var mock = RequestResponseMocker.Create();
            var api = new ResultsApi(config.JsonOptions, config.Serializer, resultTable.Object,
                userTable.Object, playerTable.Object);
            var response = await api.Get(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<List<ResultsGetDto>>(mock.ResponseBodyAsString, config.JsonOptions);

            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.Equal(2, resultPayload!.Count);
            Assert.Equal("Bar", resultPayload![0].Name);
            Assert.Equal("Foo", resultPayload![1].Name);
            Assert.Equal("https://github.com/barplayer", resultPayload![0].GitHubUrl);
            Assert.Equal("https://github.com/fooplayer", resultPayload![1].GitHubUrl);
            Assert.Equal("@foobar", resultPayload![0].PublicTwitter);
            Assert.Equal("@foobar", resultPayload![1].PublicTwitter);

            resultTable.VerifyAll();
            playerTable.VerifyAll();
            userTable.VerifyAll();
        }
    }
}
