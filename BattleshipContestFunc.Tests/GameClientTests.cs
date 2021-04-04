using Moq;
using NBattleshipCodingContest.Logic;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class GameClientTests
    {
        [Fact]
        public async Task PlaySingleMoveInRandomGame()
        {
            var gameMock = new Mock<ISinglePlayerGame>();
            gameMock.Setup(m => m.GetGameState(BattleshipBoard.Ships)).Returns(SinglePlayerGameState.InProgress);
            gameMock.Setup(m => m.Shoot(It.IsAny<BoardIndex>()));

            var gameFactoryMock = new Mock<ISinglePlayerGameFactory>();
            gameFactoryMock.Setup(m => m.Create(It.IsAny<int>())).Returns(gameMock.Object);

            var playerClientMock = new Mock<IPlayerClient>();
            playerClientMock.Setup(m => m.GetShot("https://someserver.com/api/", gameMock.Object, "key"))
                .ReturnsAsync(new BoardIndex());

            var gameClient = new GameClient(playerClientMock.Object, gameFactoryMock.Object);
            await gameClient.PlaySingleMoveInRandomGame("https://someserver.com/api/", "key");

            gameFactoryMock.VerifyAll();
            playerClientMock.VerifyAll();
        }

        [Fact]
        public async Task PlayGame()
        {
            var gameMock = new Mock<ISinglePlayerGame>();
            var counter = 0;
            gameMock.Setup(m => m.GetGameState(BattleshipBoard.Ships)).Returns(() =>
            {
                if (++counter > 10) return SinglePlayerGameState.AllShipsSunken;
                return SinglePlayerGameState.InProgress;
            });
            gameMock.Setup(m => m.Shoot(It.IsAny<BoardIndex>()));

            var gameFactoryMock = new Mock<ISinglePlayerGameFactory>();
            gameFactoryMock.Setup(m => m.Create(It.IsAny<int>())).Returns(gameMock.Object);

            var playerClientMock = new Mock<IPlayerClient>();
            playerClientMock.Setup(m => m.GetShot("https://someserver.com/api/", gameMock.Object, "key"))
                .ReturnsAsync(new BoardIndex());

            var gameClient = new GameClient(playerClientMock.Object, gameFactoryMock.Object);
            var callbackCalled = false;
            await gameClient.PlayGame("https://someserver.com/api/", () =>
                {
                    callbackCalled = true; 
                    return Task.CompletedTask; 
                }, "key");

            Assert.True(callbackCalled);
            gameFactoryMock.VerifyAll();
            playerClientMock.VerifyAll();
        }
    }
}
