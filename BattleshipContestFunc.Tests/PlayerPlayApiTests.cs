using Azure;
using BattleshipContestFunc.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
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
            Mock<IGameClient>? gameClientMock = null, Mock<IPlayerGameLeaseManager>? leaseManager = null,
            Mock<IPlayerLogTable>? logMock = null, Mock<IMessageSender>? messageSender = null)
        {
            return new PlayersPlayApi(playerMock.Object,
                config.JsonOptions, config.Serializer, authorize.Object,
                gameClientMock?.Object ?? Mock.Of<IGameClient>(),
                logMock?.Object ?? Mock.Of<IPlayerLogTable>(),
                resultsMock?.Object ?? Mock.Of<IPlayerResultTable>(),
                leaseManager?.Object ?? Mock.Of<IPlayerGameLeaseManager>(),
                Mock.Of<IConfiguration>(),
                messageSender?.Object ?? Mock.Of<IMessageSender>());
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
                Task.FromResult<Player?>(new Player(Guid.Empty)
                { 
                    WebApiUrl = "https://somewhere.com/api",
                    ApiKey = "key",
                    Name = "FooBar",
                    Creator = "foo" 
                }));
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
            await CreateApi(playerMock, AuthorizeMocker.GetAuthorizeMock("foo2"))
                .Play(mock.RequestMock.Object, Guid.Empty.ToString());

            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Forbidden, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PlayDependencyErrorOnException()
        {
            var gameClientMock = new Mock<IGameClient>();
            gameClientMock.Setup(m => m.GetReadyForGame(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ApplicationException("Dummy Error"));

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, authMock, gameClientMock: gameClientMock).Play(mock.RequestMock.Object, Guid.Empty.ToString());

            gameClientMock.VerifyAll();
            playerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.FailedDependency, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PlayTriesToAcquireLease()
        {
            var leaseManagerMock = new Mock<IPlayerGameLeaseManager>();
            leaseManagerMock.Setup(m => m.Acquire(Guid.Empty, It.IsAny<TimeSpan>()))
                .ThrowsAsync(new RequestFailedException("dummy"));

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            var mock = RequestResponseMocker.Create();
            await CreateApi(playerMock, authMock, leaseManager: leaseManagerMock)
                .Play(mock.RequestMock.Object, Guid.Empty.ToString());

            leaseManagerMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Conflict, mock.ResponseMock.Object.StatusCode);
        }

        [Fact]
        public async Task PlaySendsMessage()
        {
            var leaseManagerMock = new Mock<IPlayerGameLeaseManager>();
            leaseManagerMock.Setup(m => m.Acquire(Guid.Empty, It.IsAny<TimeSpan>())).ReturnsAsync("lease");

            var logTableMock = new Mock<IPlayerLogTable>();
            logTableMock.Setup(m => m.Add(It.IsAny<PlayerLog>())).ReturnsAsync(new PlayerLog(Guid.Empty));

            var senderMock = new Mock<IMessageSender>();
            senderMock.Setup(m => m.SendMessage(
                It.IsAny<PlayersPlayApi.MeasurePlayerRequestMessage>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<TimeSpan>()));

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            var mock = RequestResponseMocker.Create();
            var result = await CreateApi(playerMock, authMock, leaseManager: leaseManagerMock, 
                logMock: logTableMock, messageSender: senderMock)
                .Play(mock.RequestMock.Object, Guid.Empty.ToString());

            leaseManagerMock.VerifyAll();
            senderMock.VerifyAll();
            Assert.Equal(HttpStatusCode.Accepted, mock.ResponseMock.Object.StatusCode);
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

        private string CreateMessage(string? webApiUrl = null, DateTime? leaseTimeout = null,
            int? completedGameCount = null)
        {
            var message = new PlayersPlayApi.MeasurePlayerRequestMessage(Guid.Empty, "lease", 
                leaseTimeout ?? DateTime.UtcNow.AddMinutes(10),
                webApiUrl ?? "https://somewhere.com", "key", "FooBar", "rowkey",
                completedGameCount ?? 0);
            return JsonSerializer.Serialize(message, config.JsonOptions);
        }

        private static void CreateFunctionContextMock(out Mock<ILogger<PlayersPlayApi>> loggerMock, 
            out Mock<FunctionContext> contextMock, out Mock<IMessageSender> senderMock)
        {
            loggerMock = new Mock<ILogger<PlayersPlayApi>>();
            loggerMock.Setup(m => m.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

            var servicesMock = new Mock<IServiceProvider>();
            servicesMock.Setup(m => m.GetService(typeof(ILogger<PlayersPlayApi>))).Returns(loggerMock.Object);

            contextMock = new Mock<FunctionContext>();
            contextMock.SetupGet(m => m.InstanceServices).Returns(servicesMock.Object);

            senderMock = new Mock<IMessageSender>();
            senderMock.Setup(m => m.SendMessage(
                It.IsAny<PlayersPlayApi.MeasurePlayerRequestMessage>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<TimeSpan>()));
        }

        private static void VerifySendMessageNotCalled(Mock<IMessageSender> senderMock)
        {
            senderMock.Verify(m => m.SendMessage(
                It.IsAny<PlayersPlayApi.MeasurePlayerRequestMessage>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task AsyncGameInvalidMessage()
        {
            CreateFunctionContextMock(out var loggerMock, out var contextMock, out var senderMock);

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            await CreateApi(playerMock, authMock).AsyncGame("dummy {", contextMock.Object);

            loggerMock.VerifyAll();
            VerifySendMessageNotCalled(senderMock);
        }

        [Fact]
        public async Task AsyncGameModelValidationError()
        {
            CreateFunctionContextMock(out var loggerMock, out var contextMock, out var senderMock);

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            var message = CreateMessage("dummy");
            await CreateApi(playerMock, authMock).AsyncGame(message, contextMock.Object);

            loggerMock.VerifyAll();
            VerifySendMessageNotCalled(senderMock);
        }

        [Fact]
        public async Task AsyncGameHandlesLease()
        {
            CreateFunctionContextMock(out var _, out var contextMock, out var senderMock);

            var gameClientMock = new Mock<IGameClient>();
            gameClientMock.Setup(m => m.PlaySimultaneousGames(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Func<Task>>(), It.IsAny<string>()))
                .Throws(new Exception("dummy"));

            var message = CreateMessage(leaseTimeout: DateTime.UtcNow.AddSeconds(-1));

            var leaseManagerMock = new Mock<IPlayerGameLeaseManager>();
            leaseManagerMock.Setup(m => m.Renew(Guid.Empty, It.IsAny<string>()));
            leaseManagerMock.Setup(m => m.Release(Guid.Empty, It.IsAny<string>()));

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            await CreateApi(playerMock, authMock, gameClientMock: gameClientMock, leaseManager: leaseManagerMock)
                .AsyncGame(message, contextMock.Object);

            leaseManagerMock.VerifyAll();
            gameClientMock.VerifyAll();
            VerifySendMessageNotCalled(senderMock);
        }

        [Fact(Skip = "Currently, all games run in parallel")]
        public async Task AsyncGameSendNextMessage()
        {
            CreateFunctionContextMock(out var _, out var contextMock, out var senderMock);

            var gameClientMock = new Mock<IGameClient>();
            gameClientMock.Setup(m => m.PlaySimultaneousGames(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Func<Task>>(), It.IsAny<string>()))
                .ReturnsAsync(Enumerable.Range(0, PlayersPlayApi.ParallelGames).Select(_ => 42).ToArray());

            var logTableMock = new Mock<IPlayerLogTable>();
            logTableMock.Setup(m => m.Add(It.IsAny<PlayerLog>())).ReturnsAsync(new PlayerLog(Guid.Empty));
            logTableMock.Setup(m => m.Replace(It.IsAny<PlayerLog>())).ReturnsAsync(new PlayerLog(Guid.Empty));

            var message = CreateMessage(leaseTimeout: DateTime.UtcNow.AddSeconds(-1));

            var leaseManagerMock = new Mock<IPlayerGameLeaseManager>();
            leaseManagerMock.Setup(m => m.Renew(Guid.Empty, It.IsAny<string>()));

            var playerMock = CreatePlayerTable();
            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            await CreateApi(playerMock, authMock, gameClientMock: gameClientMock, 
                leaseManager: leaseManagerMock, logMock: logTableMock)
                .AsyncGame(message, contextMock.Object);

            leaseManagerMock.VerifyAll();
            leaseManagerMock.Verify(m => m.Release(Guid.Empty, It.IsAny<string>()), Times.Never);
            gameClientMock.VerifyAll();
            logTableMock.VerifyAll();
            VerifySendMessageNotCalled(senderMock);
        }

        [Fact]
        public async Task AsyncGameWritesResult()
        {
            CreateFunctionContextMock(out var _, out var contextMock, out var senderMock);

            var gameClientMock = new Mock<IGameClient>();
            gameClientMock.Setup(m => m.PlaySimultaneousGames(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Func<Task>>(), It.IsAny<string>()))
                .ReturnsAsync(Enumerable.Range(0, PlayersPlayApi.ParallelGames).Select(_ => 42).ToArray());

            var logTableMock = new Mock<IPlayerLogTable>();
            logTableMock.Setup(m => m.Add(It.IsAny<PlayerLog>())).ReturnsAsync(new PlayerLog(Guid.Empty));
            logTableMock.Setup(m => m.Replace(It.IsAny<PlayerLog>())).ReturnsAsync(new PlayerLog(Guid.Empty));

            var resultTableMock = new Mock<IPlayerResultTable>();
            resultTableMock.Setup(m => m.GetSingle(Guid.Empty)).ReturnsAsync(new PlayerResult(Guid.Empty));
            resultTableMock.Setup(m => m.Replace(It.Is<PlayerResult>(pr => pr.AvgNumberOfShots == 42d * PlayersPlayApi.ParallelGames / PlayersPlayApi.NumberOfGames)))
                .ReturnsAsync(new PlayerResult(Guid.Empty));

            var message = CreateMessage(leaseTimeout: DateTime.UtcNow.AddSeconds(-1), 
                completedGameCount: PlayersPlayApi.NumberOfGames - PlayersPlayApi.ParallelGames);

            var leaseManagerMock = new Mock<IPlayerGameLeaseManager>();
            leaseManagerMock.Setup(m => m.Renew(Guid.Empty, It.IsAny<string>()));
            leaseManagerMock.Setup(m => m.Release(Guid.Empty, It.IsAny<string>()));

            var playerMock = CreatePlayerTable();
            playerMock.Setup(m => m.Replace(It.Is<Player>(p => p.TournamentInProgressSince == null)));

            var authMock = AuthorizeMocker.GetAuthorizeMock("foo");
            await CreateApi(playerMock, authMock, gameClientMock: gameClientMock,
                leaseManager: leaseManagerMock, logMock: logTableMock, resultsMock: resultTableMock)
                .AsyncGame(message, contextMock.Object);

            leaseManagerMock.VerifyAll();
            gameClientMock.VerifyAll();
            logTableMock.VerifyAll();
            resultTableMock.VerifyAll();
            playerMock.VerifyAll();
            VerifySendMessageNotCalled(senderMock);
        }
    }
}
