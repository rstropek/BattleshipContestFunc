using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
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

        private BattleshipContestFunc.PlayersApi CreateApi(Mock<IPlayerTable> playerMock)
        {
            var authorizeMock = new Mock<IAuthorize>();
            authorizeMock.Setup(a => a.GetUser(It.IsAny<HttpHeadersCollection>()))
                .Returns(Task.FromResult<ClaimsPrincipal?>(null));
            return CreateApi(playerMock, authorizeMock);
        }

        private BattleshipContestFunc.PlayersApi CreateApi(Mock<IPlayerTable> playerMock,
            Mock<IAuthorize> authorize, Mock<IPlayerResultTable>? resultsMock = null)
        {
            return new BattleshipContestFunc.PlayersApi(playerMock.Object, config.Mapper,
                config.JsonOptions, config.Serializer, authorize.Object, Mock.Of<IGameClient>(),
                Mock.Of<IPlayerLogTable>(), resultsMock?.Object ?? Mock.Of<IPlayerResultTable>(),
                Mock.Of<IPlayerGameLeaseManager>());
        }

        public static Mock<IPlayerResultTable> CreateEmptyResultTableMock()
        {
            var mock = new Mock<IPlayerResultTable>();
            mock.Setup(t => t.Get(null)).Returns(Task.FromResult(new List<PlayerResult>()));
            return mock;
        }

        [Fact]
        public async Task Get()
        {
            var payload = new List<Player>
            {
                new(Guid.Empty) { Name = "Dummy", WebApiUrl = "https://somewhere.com/api", Creator = "foo" }
            };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Get(It.IsAny<Expression<Func<Player, bool>>>()))
                .Returns(Task.FromResult(payload));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo"), CreateEmptyResultTableMock()).Get(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<List<PlayerGetDto>>(mock.ResponseBodyAsString, config.JsonOptions);

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.Single(resultPayload);
            Assert.Equal(new Guid(payload[0].RowKey), resultPayload![0].Id);
            Assert.Equal(payload[0].Name, resultPayload[0].Name);
            Assert.Equal(payload[0].WebApiUrl, resultPayload[0].WebApiUrl);
            Assert.Equal(payload[0].Creator, resultPayload[0].Creator);
        }

        [Fact]
        public async Task GetWithApiKey()
        {
            var payload = new List<Player>
            {
                new(Guid.Empty) { Name = "Dummy", WebApiUrl = "https://somewhere.com/api", Creator = "foo", ApiKey = "dummy" },
                new(Guid.Empty) { Name = "Dummy", WebApiUrl = "https://somewhere.com/api", Creator = "foo" }
            };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Get(It.IsAny<Expression<Func<Player, bool>>>()))
                .Returns(Task.FromResult(payload));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo"), CreateEmptyResultTableMock()).Get(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<List<PlayerGetDto>>(mock.ResponseBodyAsString, config.JsonOptions);

            Assert.True(resultPayload![0].HasApiKey);
            Assert.False(resultPayload![1].HasApiKey);
        }

        private static PlayerAddDto GetEmptyAddDto() => new(Guid.Empty, string.Empty, string.Empty);

        [Fact]
        public async Task GetChecksAuthorization()
        {
            var authMock = AuthorizeMocker.GetUnauthorizedMock();
            var mock = RequestResponseMocker.Create(GetEmptyAddDto(), config.JsonOptions);
            await CreateApi(new Mock<IPlayerTable>(), authMock).Get(mock.RequestMock.Object);

            authMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetSingleInvalidId()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).GetSingle(mock.RequestMock.Object, "dummy");

            playerMock.Verify(p => p.GetSingle(It.IsAny<Guid>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetSingleNotFound()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).GetSingle(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.NotFound, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetSingleChecksAuthorization()
        {
            var authMock = AuthorizeMocker.GetUnauthorizedMock();
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, authMock).GetSingle(mock.RequestMock.Object, Guid.Empty.ToString());

            authMock.VerifyAll();
            playerMock.Verify(p => p.GetSingle(It.IsAny<Guid>()), Times.Never);
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task GetSingle()
        {
            var payload = new Player(Guid.Empty)
            { 
                Name = "Dummy", WebApiUrl = "https://somewhere.com/api", Creator = "foo",
                GitHubUrl = "https://github.com/asdf"
            };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>())).Returns(Task.FromResult<Player?>(payload));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).GetSingle(mock.RequestMock.Object, Guid.Empty.ToString());
            var resultPayload = JsonSerializer.Deserialize<PlayerGetDto>(mock.ResponseBodyAsString, config.JsonOptions);

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.Equal(new Guid(payload.RowKey), resultPayload!.Id);
            Assert.Equal(payload.Name, resultPayload.Name);
            Assert.Equal(payload.WebApiUrl, resultPayload.WebApiUrl);
            Assert.Equal(payload.Creator, resultPayload.Creator);
            Assert.Equal(payload.GitHubUrl, resultPayload.GitHubUrl);
        }

        [Fact]
        public async Task GetSingleForeignPlayer()
        {
            var payload = new Player(Guid.Empty)
            {
                Name = "Dummy",
                WebApiUrl = "https://somewhere.com/api",
                Creator = "foo"
            };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>())).Returns(Task.FromResult<Player?>(payload));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo2")).GetSingle(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Forbidden, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task AddInvalidBody()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Add(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create("dummy {");
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Add(mock.RequestMock.Object);

            playerMock.Verify(p => p.Add(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task AddFailingValidation()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Add(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(GetEmptyAddDto(), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Add(mock.RequestMock.Object);

            playerMock.Verify(p => p.Add(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task AddChecksAuthorization()
        {
            var authMock = AuthorizeMocker.GetUnauthorizedMock();
            var mock = RequestResponseMocker.Create(GetEmptyAddDto(), config.JsonOptions);
            await CreateApi(new Mock<IPlayerTable>(), authMock).Add(mock.RequestMock.Object);

            authMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task Add()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Add(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(
                new PlayerAddDto(Guid.Empty, "Dummy", "https://someserver.com/api?x=a", "C0d€", "https://github.com/asdf"), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Add(mock.RequestMock.Object);
            var resultPayload = JsonSerializer.Deserialize<PlayerGetDto>(mock.ResponseBodyAsString, config.JsonOptions);

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Created, mock.ResponseMock.Object.StatusCode);
            Assert.StartsWith("application/json", mock.Headers.First(h => h.Key == "Content-Type").Value.First());
            Assert.NotNull(resultPayload);
            Assert.NotEqual(Guid.Empty, resultPayload!.Id);
            Assert.Equal("Dummy", resultPayload.Name);
            Assert.Equal("https://someserver.com/api?x=a", resultPayload.WebApiUrl);
            Assert.Equal("foo", resultPayload.Creator);
            Assert.Equal("https://github.com/asdf", resultPayload.GitHubUrl);
        }

        [Fact]
        public async Task DeleteInvalidId()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Delete(It.IsAny<Guid>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Delete(mock.RequestMock.Object, "dummy");

            playerMock.Verify(p => p.Delete(It.IsAny<Guid>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task Delete()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(Guid.Empty)).Returns(Task.FromResult<Player?>(
                new Player(Guid.Empty) { Creator = "foo" }));
            playerMock.Setup(p => p.Delete(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Delete(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.NoContent, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task DeleteChecksAuthorization()
        {
            var authMock = AuthorizeMocker.GetUnauthorizedMock();
            var mock = RequestResponseMocker.Create();
            await CreateApi(new Mock<IPlayerTable>(), authMock).Delete(mock.RequestMock.Object, Guid.Empty.ToString());

            authMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task DeleteForeignPlayer()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(Guid.Empty)).Returns(Task.FromResult<Player?>(
                new Player(Guid.Empty) { Creator = "foo1" }));
            playerMock.Setup(p => p.Delete(Guid.Empty));

            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo2")).Delete(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.Verify(p => p.Delete(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.Forbidden, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PatchChecksAuthorization()
        {
            var authMock = AuthorizeMocker.GetUnauthorizedMock();
            var mock = RequestResponseMocker.Create();
            await CreateApi(new Mock<IPlayerTable>(), authMock).Patch(mock.RequestMock.Object, Guid.Empty.ToString());

            authMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PatchInvalidBody()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Replace(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create("dummy {");
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.Verify(p => p.Replace(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PatchFailingValidation()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Replace(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(GetEmptyAddDto() with { WebApiUrl = "a/b" }, config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.Verify(p => p.Replace(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PatchInvalidId()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.Replace(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(new PlayerPatchDto("asdf", "https://asdf.com"), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object, "dummy");

            playerMock.Verify(p => p.Replace(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PatchForeignPlayer()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(Guid.Empty)).Returns(Task.FromResult<Player?>(
                new Player(Guid.Empty) { Creator = "foo1" }));
            playerMock.Setup(p => p.Replace(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(new PlayerPatchDto("asdf", "https://asdf.com"), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo2")).Patch(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.Verify(p => p.Replace(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.Forbidden, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task NoUpdateIfNothingChanged()
        {
            var data = new Player(Guid.Empty) { Name = "asdf", Creator = "foo", WebApiUrl = "https://dummy.com" };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(Guid.Empty)).Returns(Task.FromResult<Player?>(data));
            playerMock.Setup(p => p.Replace(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(new PlayerPatchDto("asdf", "https://dummy.com"), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.Verify(p => p.Replace(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task UpdateEmptyName()
        {
            var data = new Player(Guid.Empty) { Name = "asdf", Creator = "foo", WebApiUrl = "https://dummy.com" };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(Guid.Empty)).Returns(Task.FromResult<Player?>(data));
            playerMock.Setup(p => p.Replace(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(new PlayerPatchDto(Name: ""), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.Verify(p => p.Replace(It.IsAny<Player>()), Times.Never);
            Assert.Equal(HttpStatusCode.BadRequest, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task Update()
        {
            var data = new Player(Guid.Empty) { Name = "a", Creator = "foo", WebApiUrl = "https://dummy.com" };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(Guid.Empty)).Returns(Task.FromResult<Player?>(data));
            playerMock.Setup(p => p.Replace(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(new PlayerPatchDto("ab", "https://new.com", "key"), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object, Guid.Empty.ToString());

            Expression<Func<Player, bool>> verify = p => p.Name == "ab" && p.WebApiUrl == "https://new.com"
                && p.ApiKey == "key";

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task UpdateSingleField()
        {
            var data = new Player(Guid.Empty) { Name = "a", Creator = "foo", WebApiUrl = "https://dummy.com" };
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(Guid.Empty)).Returns(Task.FromResult<Player?>(data));
            playerMock.Setup(p => p.Replace(It.IsAny<Player>()));

            var mock = RequestResponseMocker.Create(new PlayerPatchDto(null, "https://new.com", "key"), config.JsonOptions);
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo")).Patch(mock.RequestMock.Object, Guid.Empty.ToString());

            Expression<Func<Player, bool>> verify = p => p.Name == "a" && p.WebApiUrl == "https://new.com"
                && p.ApiKey == "key";

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }
    }
}
