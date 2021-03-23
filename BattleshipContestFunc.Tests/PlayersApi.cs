using BattleshipContestFunc.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class PlayersApi : IClassFixture<ApiConfigFixture>
    {
        private readonly ApiConfigFixture config;

        public PlayersApi(ApiConfigFixture config)
        {
            this.config = config;
        }

        [Fact]
        public async Task Get()
        {
            var payload = new List<Player>
            {
                new(Guid.Empty) { Name = "Dummy", WebApiUrl = "https://somewhere.com/api" }
            };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Get(null)).Returns(Task.FromResult(payload));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock).Get(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<List<PlayerDto>>(mock.ResponseBodyAsString, config.JsonOptions);

            playerMock.Verify(p => p.Get(null), Times.Once);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.Single(resultPayload);
            Assert.Equal(new Guid(payload[0].RowKey), resultPayload![0].Id);
            Assert.Equal(payload[0].Name, resultPayload[0].Name);
            Assert.Equal(payload[0].WebApiUrl, resultPayload[0].WebApiUrl);
        }

        private BattleshipContestFunc.PlayersApi CreateApi(Mock<IPlayerTable> playerMock)
        {
            return new BattleshipContestFunc.PlayersApi(playerMock.Object, config.Mapper,
                config.JsonOptions, config.Serializer);
        }

        [Fact]
        public async Task GetSingleInvalidId()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock).GetSingle(mock.RequestMock.Object, "dummy");

            playerMock.Verify(p => p.GetSingle(It.IsAny<Guid>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetSingleNotFound()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>())).Returns(Task.FromResult<Player?>(null));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock).GetSingle(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.Verify(p => p.GetSingle(It.IsAny<Guid>()), Times.Once);
            Assert.Equal(HttpStatusCode.NotFound, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetSingle()
        {
            var payload = new Player(Guid.Empty) { Name = "Dummy", WebApiUrl = "https://somewhere.com/api" };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>())).Returns(Task.FromResult<Player?>(payload));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock).GetSingle(mock.RequestMock.Object, Guid.Empty.ToString());
            var resultPayload = JsonSerializer.Deserialize<PlayerDto>(mock.ResponseBodyAsString, config.JsonOptions);

            playerMock.Verify(p => p.GetSingle(It.IsAny<Guid>()), Times.Once);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.Equal(new Guid(payload.RowKey), resultPayload!.Id);
            Assert.Equal(payload.Name, resultPayload.Name);
            Assert.Equal(payload.WebApiUrl, resultPayload.WebApiUrl);
        }

        [Fact]
        public async Task AddInvalidBody()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Add(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create("dummy {");
            await CreateApi(playerMock).Add(mock.RequestMock.Object);

            playerMock.Verify(p => p.Add(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task AddEmptyName()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Add(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(JsonSerializer.Serialize(
                new PlayerDto(Guid.Empty, string.Empty, string.Empty), config.JsonOptions));
            await CreateApi(playerMock).Add(mock.RequestMock.Object);

            playerMock.Verify(p => p.Add(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task AddEmptyWebApi()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Add(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(JsonSerializer.Serialize(
                new PlayerDto(Guid.Empty, "Dummy", string.Empty), config.JsonOptions));
            await CreateApi(playerMock).Add(mock.RequestMock.Object);

            playerMock.Verify(p => p.Add(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task AddInvalidWebApi()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Add(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(JsonSerializer.Serialize(
                new PlayerDto(Guid.Empty, "Dummy", "some/api"), config.JsonOptions));
            await CreateApi(playerMock).Add(mock.RequestMock.Object);

            playerMock.Verify(p => p.Add(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task Add()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Add(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(JsonSerializer.Serialize(
                new PlayerDto(Guid.Empty, "Dummy", "https://someserver.com/api?x=a b"), config.JsonOptions));
            await CreateApi(playerMock).Add(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<PlayerDto>(mock.ResponseBodyAsString, config.JsonOptions);

            Expression<Func<Player, bool>> playerCheck = p => p.Name == "Dummy" && p.WebApiUrl.Contains("%20");

            playerMock.Verify(p => p.Add(It.Is(playerCheck)), Times.Once);
            Assert.Equal(HttpStatusCode.Created, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.NotEqual(Guid.Empty, resultPayload!.Id);
            Assert.Equal("Dummy", resultPayload.Name);
            Assert.Contains("%20", resultPayload.WebApiUrl);
        }

        [Fact]
        public async Task DeleteInvalidId()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Delete(It.IsAny<Guid>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock).Delete(mock.RequestMock.Object, "dummy");

            playerMock.Verify(p => p.Delete(It.IsAny<Guid>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task Delete()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(Guid.Empty)).Returns(Task.FromResult<Player?>(new Player(Guid.Empty)));
            playerMock.Setup(p => p.Delete(Guid.Empty));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock).Delete(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.Verify(p => p.Delete(It.IsAny<Player>()), Times.Once);
            Assert.Equal(HttpStatusCode.NoContent, mock.ResponseMock.Object.StatusCode);
        }
    }
}
