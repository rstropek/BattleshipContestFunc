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

        public async Task GetReadyForGame(string playerWebApiUrl, string? apiKey = null)
            => await playerClient.GetReady(playerWebApiUrl, apiKey);

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

            await playerClient.GetShot(playerWebApiUrl, game, apiKey);
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

        public async Task<IEnumerable<int>> PlaySimultaneousGames(string playerWebApiUrl, int parallelGames = 5, Func<Task>? postRoundCallback = null, string? apiKey = null)
        {
            var games = new ISinglePlayerGame[parallelGames];
            for (var i = 0; i < parallelGames; i++) games[i] = gameFactory.Create(0);

            var runningGames = games.ToArray();
            while (runningGames.Length > 0)
            {
                var shots = await playerClient.GetShots(playerWebApiUrl, runningGames, apiKey);
                for (var i = 0; i < runningGames.Length; i++) runningGames[i].Shoot(shots[i]);
                runningGames = runningGames.Where(g => g.GetGameState(BattleshipBoard.Ships) == SinglePlayerGameState.InProgress).ToArray();
                if (postRoundCallback != null) await postRoundCallback();
            }

            return games.Select(g => g.NumberOfShots).ToArray();
        }
    }
}
