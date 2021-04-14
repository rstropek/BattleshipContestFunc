using NBattleshipCodingContest.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("BattleshipContestFunc.Tests")]

namespace BattleshipContestFunc
{
    public class GameClient : IGameClient
    {
        private readonly IPlayerClient playerClient;
        private readonly ISinglePlayerGameFactory gameFactory;

        public GameClient(IPlayerClient playerClient, ISinglePlayerGameFactory gameFactory)
        {
            this.playerClient = playerClient;
            this.gameFactory = gameFactory;
        }

        public async Task GetReadyForGame(string playerWebApiUrl, int numberOfGames, string? apiKey = null)
            => await playerClient.GetReady(playerWebApiUrl, numberOfGames, apiKey);

        public async Task PlaySingleMoveInRandomGame(string playerWebApiUrl, string? apiKey = null)
        {
            static void ShootSomeRandomShots(ISinglePlayerGame game)
            {
                var rand = new Random();
                while (true)
                {
                    for (var i = rand.Next(10, 30); i > 0; i--) game.Shoot(new BoardIndex(rand.Next(10), rand.Next(10)));
                    if (game.GetGameState(BattleshipBoard.Ships) == SinglePlayerGameState.InProgress) break;
                }
            }

            var game = gameFactory.Create(0);
            ShootSomeRandomShots(game);

            await playerClient.GetShots(playerWebApiUrl, new[] { game }, apiKey);
        }

        public async Task<int> PlayGame(string playerWebApiUrl, Func<Task>? postRoundCallback = null, string? apiKey = null)
        {
            var game = gameFactory.Create(0);
            while (game.GetGameState(BattleshipBoard.Ships) == SinglePlayerGameState.InProgress)
            {
                var shot = await playerClient.GetShot(playerWebApiUrl, game, apiKey);
                game.Shoot(shot);
                if (postRoundCallback != null) await postRoundCallback();
            }

            return game.NumberOfShots;
        }

        public IReadOnlyList<SinglePlayerGame> CreateTournamentGames(int numberOfGames)
        {
            var result = new List<SinglePlayerGame>(numberOfGames);
            for (var i = 0; i < numberOfGames; i++)
            {
                result.Add((SinglePlayerGame)gameFactory.Create(0));
            }

            return result;
        }

        public async Task PlaySimultaneousGames(string playerWebApiUrl, IEnumerable<SinglePlayerGame> games,
            int maximumShots, Func<Task>? postRoundCallback = null, string? apiKey = null,
            int[]? ships = null)
        {
            for (var shot = 0; shot < maximumShots; shot++)
            {
                var runningGames = games
                    .Where(g => g.GetGameState(ships ?? BattleshipBoard.Ships) == SinglePlayerGameState.InProgress)
                    .ToList();
                if (runningGames.Count == 0) break;

                var shots = await playerClient.GetShots(playerWebApiUrl, runningGames, apiKey);
                for (var i = 0; i < runningGames.Count; i++) runningGames[i].Shoot(shots[i]);

                if (postRoundCallback != null) await postRoundCallback();
            }
        }
        public async Task NotifyGameFinished(string playerWebApiUrl, IEnumerable<SinglePlayerGame> games, string? apiKey = null)
            => await playerClient.Finished(playerWebApiUrl, games, apiKey);
    }
}
