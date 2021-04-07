using NBattleshipCodingContest.Logic;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class SequentialPlayerApi : IClassFixture<ApiConfigFixture>
    {
        private readonly ApiConfigFixture config;

        public SequentialPlayerApi(ApiConfigFixture config)
        {
            this.config = config;
        }

        [Fact]
        public void GetReadyRandomPlayer()
        {
            var mock = RequestResponseMocker.Create();

            var api = new Players.SequentialPlayerApi(config.JsonOptions, config.Serializer);
            api.GetReady(mock.RequestMock.Object);

            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetShots()
        {
            var shotRequests = new[]
            {
                new ShotRequest(null, new BoardContent().ToShortString())
            };
            var mock = RequestResponseMocker.Create(JsonSerializer.Serialize(shotRequests, config.JsonOptions));
            var api = new Players.RandomPlayerApi(config.JsonOptions, config.Serializer);
            await api.GetShots(mock.RequestMock.Object);
            
            JsonSerializer.Deserialize<BoardIndex[]>(mock.ResponseBodyAsString, config.JsonOptions);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }
    }
}
