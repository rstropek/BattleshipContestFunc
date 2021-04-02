using NBattleshipCodingContest.Logic;
using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public class GameClient : IGameClient
    {
        private readonly IBoardFiller filler;
        private readonly IPlayerClient playerClient;

        public GameClient(IBoardFiller filler, IPlayerClient playerClient)
        {
            this.filler = filler;
            this.playerClient = playerClient;
        }

        public async Task GetReadyForGame(string playerWebApiUrl, string? apiKey = null)
            => await playerClient.GetReady(playerWebApiUrl, apiKey);

        public async Task PlaySingleMoveInRandomGame(string playerWebApiUrl, string? apiKey = null)
        {
            static void ShootSomeRandomShots(SinglePlayerGame game)
            {
                var rand = new Random();
                while (true)
                {
                    for (var i = rand.Next(10, 30); i > 0; i--) game.Shoot(new BoardIndex(rand.Next(10), rand.Next(10)));
                    if (game.GetGameState(BattleshipBoard.Ships) == SinglePlayerGameState.InProgress) break;
                }
            }

            var board = new BattleshipBoard();
            filler.Fill(BattleshipBoard.Ships, board);
            var game = new SinglePlayerGame(Guid.Empty, 0, board, new BoardContent(SquareContent.Unknown));
            ShootSomeRandomShots(game);

            await playerClient.GetShot(playerWebApiUrl, game, apiKey);
        }

        public async Task<int> PlayGame(string playerWebApiUrl, Func<Task>? postRoundCallback = null, string? apiKey = null)
        {
            var board = new BattleshipBoard();
            filler.Fill(BattleshipBoard.Ships, board);
            var game = new SinglePlayerGame(Guid.Empty, 0, board, new BoardContent(SquareContent.Unknown));

            while (game.GetGameState(BattleshipBoard.Ships) == SinglePlayerGameState.InProgress)
            {
                var shot = await playerClient.GetShot(playerWebApiUrl, game, apiKey);
                game.Shoot(shot);
                if (postRoundCallback != null) await postRoundCallback();
            }

            return game.NumberOfShots;
        }
    }
}
