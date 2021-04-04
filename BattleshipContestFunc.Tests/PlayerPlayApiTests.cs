using BattleshipContestFunc.Data;
using Moq;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class PlayerPlayApiTests : IClassFixture<ApiConfigFixture>
    {
        private readonly ApiConfigFixture config;

        public PlayerPlayApiTests(ApiConfigFixture config)
        {
            this.config = config;
        }

        private PlayersPlayApi CreateApi(Mock<IPlayerTable> playerMock,
            Mock<IAuthorize> authorize, Mock<IPlayerResultTable>? resultsMock = null,
            Mock<IGameClient>? gameClientMock = null, Mock<IPlayerGameLeaseManager>? leaseManager = null)
        {
            return new PlayersPlayApi(playerMock.Object,
                config.JsonOptions, config.Serializer, authorize.Object,
                gameClientMock?.Object ?? Mock.Of<IGameClient>(),
                Mock.Of<IPlayerLogTable>(), resultsMock?.Object ?? Mock.Of<IPlayerResultTable>(),
                leaseManager?.Object ?? Mock.Of<IPlayerGameLeaseManager>());
        }

        [Fact]
        public async Task TestChecksAuthorization()
        {
            var authMock = AuthorizeMocker.GetUnauthorizedMock();
            var mock = RequestResponseMocker.Create();
            await CreateApi(new Mock<IPlayerTable>(), authMock).Test(mock.RequestMock.Object, Guid.Empty.ToString());

            authMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task TestForeignPlayer()
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
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo2")).Test(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Forbidden, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task TestGetsReadyAndPlays()
        {
            var gameClientMock = new Mock<IGameClient>();
            gameClientMock.Setup(m => m.GetReadyForGame(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            gameClientMock.Setup(m => m.PlaySingleMoveInRandomGame(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, authMock, gameClientMock: gameClientMock).Test(mock.RequestMock.Object, Guid.Empty.ToString());

            gameClientMock.VerifyAll();
            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.OK, mock.ResponseMock.Object.StatusCode);
        }

        private static Mock<IPlayerTable> CreatePlayerTable()
        {
            var playerMock = new Mock<IPlayerTable>();
            playerMock.Setup(p => p.GetSingle(It.IsAny<Guid>())).Returns(
                Task.FromResult<Player?>(new Player(Guid.Empty) { Creator = "foo" }));
            return playerMock;
        }

        [Fact]
        public async Task TestDependencyErrorOnException()
        {
            var gameClientMock = new Mock<IGameClient>();
            gameClientMock.Setup(m => m.GetReadyForGame(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ApplicationException("Dummy Error"));

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, authMock, gameClientMock: gameClientMock).Test(mock.RequestMock.Object, Guid.Empty.ToString());

            gameClientMock.VerifyAll();
            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.FailedDependency, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PlayChecksAuthorization()
        {
            var authMock = AuthorizeMocker.GetUnauthorizedMock();
            var mock = RequestResponseMocker.Create();
            await CreateApi(new Mock<IPlayerTable>(), authMock).Play(mock.RequestMock.Object, Guid.Empty.ToString());

            authMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Unauthorized, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PlayForeignPlayer()
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
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo2")).Test(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Forbidden, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task RenewLease()
        {
            var leaseManagerMock = new Mock<IPlayerGameLeaseManager>();
            leaseManagerMock.Setup(m => m.Renew(Guid.Empty, "foo"));

            var api = CreateApi(new Mock<IPlayerTable>(), AuthorizeMocker.GetUnauthorizedMock(), leaseManager: leaseManagerMock);

            var msg = CreateDummyMeasurePlayerRequestMessage(DateTime.UtcNow.AddSeconds(-1));
            msg = await api.RenewLease(msg);

            leaseManagerMock.VerifyAll();
            Assert.True(msg.LeaseEnd > DateTime.UtcNow);
        }

        private static PlayersPlayApi.MeasurePlayerRequestMessage CreateDummyMeasurePlayerRequestMessage(DateTime leaseEnd)
            => new(Guid.Empty, "foo", leaseEnd, "https://somewhere.com", "key", "bar", "foobar");

        [Fact]
        public async Task RenewLeaseForced()
        {
            var leaseManagerMock = new Mock<IPlayerGameLeaseManager>();
            leaseManagerMock.Setup(m => m.Renew(Guid.Empty, "foo"));

            var api = CreateApi(new Mock<IPlayerTable>(), AuthorizeMocker.GetUnauthorizedMock(), leaseManager: leaseManagerMock);

            var msg = CreateDummyMeasurePlayerRequestMessage(DateTime.UtcNow.AddSeconds(10));
            msg = await api.RenewLease(msg, true);

            leaseManagerMock.VerifyAll();
            Assert.True(msg.LeaseEnd > DateTime.UtcNow);
        }

        [Fact]
        public async Task RenewLeaseIgnored()
        {
            var leaseManagerMock = new Mock<IPlayerGameLeaseManager>();
            leaseManagerMock.Setup(m => m.Renew(It.IsAny<Guid>(), It.IsAny<string>()));

            var api = CreateApi(new Mock<IPlayerTable>(), AuthorizeMocker.GetUnauthorizedMock(), leaseManager: leaseManagerMock);

            var msg = CreateDummyMeasurePlayerRequestMessage(DateTime.UtcNow.AddSeconds(10));
            var msgNew = await api.RenewLease(msg);

            leaseManagerMock.Verify(m => m.Renew(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            Assert.Equal(msg, msgNew);
        }
    }
}
