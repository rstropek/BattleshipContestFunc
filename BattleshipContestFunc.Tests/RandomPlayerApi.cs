using NBattleshipCodingContest.Logic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class RandomPlayerApi : IClassFixture<ApiConfigFixture>
    {
        private readonly ApiConfigFixture config;

        public RandomPlayerApi(ApiConfigFixture config)
        {
            this.config = config;
        }

        [Fact]
        public void GetReadyRandomPlayer()
        {
            var mock = RequestResponseMocker.Create();

            var api = new BattleshipContestFunc.RandomPlayerApi(config.JsonOptions, config.Serializer);
            api.GetReady(mock.RequestMock.Object);

            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetShotInvalidBody()
        {
            var mock = RequestResponseMocker.Create("dummy {");
            var api = new BattleshipContestFunc.RandomPlayerApi(config.JsonOptions, config.Serializer);
            await api.GetShot(mock.RequestMock.Object);

            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetShot()
        {
            var mock = RequestResponseMocker.Create(JsonSerializer.Serialize(new BoardContent(), config.JsonOptions));
            var api = new BattleshipContestFunc.RandomPlayerApi(config.JsonOptions, config.Serializer);
            await api.GetShot(mock.RequestMock.Object);
            
            JsonSerializer.Deserialize<BoardIndex>(mock.ResponseBodyAsString, config.JsonOptions);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }
    }
}
