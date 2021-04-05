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

            var api = new BattleshipContestFunc.SequentialPlayerApi(config.JsonOptions, config.Serializer);
            api.GetReady(mock.RequestMock.Object);

            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetShot()
        {
            var shotRequest = new ShotRequest(new List<SinglePlayerGameLogRecord>(), null, new BoardContent().ToShortString());
            var mock = RequestResponseMocker.Create(JsonSerializer.Serialize(shotRequest, config.JsonOptions));
            var api = new BattleshipContestFunc.RandomPlayerApi(config.JsonOptions, config.Serializer);
            await api.GetShot(mock.RequestMock.Object);
            
            JsonSerializer.Deserialize<BoardIndex>(mock.ResponseBodyAsString, config.JsonOptions);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }
    }
}
