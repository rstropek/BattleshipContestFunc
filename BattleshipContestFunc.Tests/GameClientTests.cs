using Grpc.Core;
using Microsoft.OData.UriParser;
using Moq;
using NBattleshipCodingContest.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public void CreateTournamentGames()
        {
            var gameFactoryMock = new Mock<ISinglePlayerGameFactory>();
            var client = new GameClient(Mock.Of<IPlayerClient>(), gameFactoryMock.Object);

            var games = client.CreateTournamentGames(5);

            Assert.Equal(5, games.Count());
            gameFactoryMock.Verify(m => m.Create(0), Times.Exactly(5));
        }

        [Fact]
        public async Task PlaySimultaneousGamesMaximumShots()
        {
            var board = new BoardContent(SquareContent.Water);
            board["A1"] = board["C1"] = board["E1"] = SquareContent.Ship;
            var shootingBoard = new BoardContent(SquareContent.Unknown);
            var game = new SinglePlayerGame(Guid.Empty, 0, board, shootingBoard);
            var games = new[] { game };

            var playerClientMock = new Mock<IPlayerClient>();
            playerClientMock.Setup(m => m.GetShots("https://someserver.com/api/", 
                It.Is<IEnumerable<ISinglePlayerGame>>(g => g.Count() == 1), "key"))
                .ReturnsAsync(new[] { new BoardIndex() });

            var client = new GameClient(playerClientMock.Object, Mock.Of<ISinglePlayerGameFactory>());
            await client.PlaySimultaneousGames("https://someserver.com/api/", games, 2, null, "key", new[] { 1, 1, 1 });

            playerClientMock.Verify(m => m.GetShots("https://someserver.com/api/",
                It.Is<IEnumerable<ISinglePlayerGame>>(g => g.Count() == 1), "key"), Times.Exactly(2));
        }

        [Fact]
        public async Task PlaySimultaneousGamesStopsAtWin()
        {
            var board = new BoardContent(SquareContent.Water);
            board["A1"] = board["C1"] = board["E1"] = SquareContent.Ship;
            var shootingBoard = new BoardContent(SquareContent.Unknown);
            shootingBoard["C1"] = shootingBoard["E1"] = SquareContent.SunkenShip;
            var game = new SinglePlayerGame(Guid.Empty, 0, board, shootingBoard);
            var games = new[] { game };

            var playerClientMock = new Mock<IPlayerClient>();
            playerClientMock.Setup(m => m.GetShots("https://someserver.com/api/",
                It.Is<IEnumerable<ISinglePlayerGame>>(g => g.Count() == 1), "key"))
                .ReturnsAsync(new[] { new BoardIndex() });

            var client = new GameClient(playerClientMock.Object, Mock.Of<ISinglePlayerGameFactory>());
            await client.PlaySimultaneousGames("https://someserver.com/api/", games, 3, null, "key", new[] { 1, 1, 1 });

            playerClientMock.Verify(m => m.GetShots("https://someserver.com/api/",
                It.Is<IEnumerable<ISinglePlayerGame>>(g => g.Count() == 1), "key"), Times.Once);
        }

        [Fact]
        public async Task PlaySimultaneousGamesCallbackCalled()
        {
            var board = new BoardContent(SquareContent.Water);
            board["A1"] = board["C1"] = board["E1"] = SquareContent.Ship;
            var shootingBoard = new BoardContent(SquareContent.Unknown);
            var game = new SinglePlayerGame(Guid.Empty, 0, board, shootingBoard);
            var games = new[] { game };

            var playerClientMock = new Mock<IPlayerClient>();
            playerClientMock.Setup(m => m.GetShots(It.IsAny<string>(), 
                It.IsAny<IEnumerable<ISinglePlayerGame>>(), It.IsAny<string?>()))
                .ReturnsAsync(new[] { new BoardIndex() });

            var callbackCalls = 0;
            var client = new GameClient(playerClientMock.Object, Mock.Of<ISinglePlayerGameFactory>());
            Task Callback()
            {
                callbackCalls++;
                return Task.CompletedTask;
            }
            await client.PlaySimultaneousGames("https://someserver.com/api/", games, 2, Callback, "key", new[] { 1, 1, 1 });

            Assert.Equal(2, callbackCalls);
        }
    }
}
