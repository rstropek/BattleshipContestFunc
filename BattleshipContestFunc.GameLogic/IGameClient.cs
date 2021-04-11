using NBattleshipCodingContest.Logic;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IGameClient 
    {
        Task GetReadyForGame(string playerWebApiUrl, string? apiKey = null);

        Task<int> PlayGame(string playerWebApiUrl, Func<Task>? postRoundCallback = null, string? apiKey = null);

        IReadOnlyList<SinglePlayerGame> CreateTournamentGames(int numberOfGames);

        Task PlaySimultaneousGames(string playerWebApiUrl, IEnumerable<SinglePlayerGame> games,
            int maximumShots, Func<Task>? postRoundCallback = null, string? apiKey = null, int[] ? ships = null);

        Task PlaySingleMoveInRandomGame(string playerWebApiUrl, string? apiKey = null);
    }
}
