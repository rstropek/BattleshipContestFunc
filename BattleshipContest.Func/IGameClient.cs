using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IGameClient 
    {
        Task GetReadyForGame(string playerWebApiUrl, string? apiKey = null);

        Task<int> PlayGame(string playerWebApiUrl, Func<Task>? postRoundCallback = null, string? apiKey = null);

        Task<IEnumerable<int>> PlaySimultaneousGames(string playerWebApiUrl, int parallelGames = 5, Func<Task>? postRoundCallback = null, string? apiKey = null);

        Task PlaySingleMoveInRandomGame(string playerWebApiUrl, string? apiKey = null);
    }
}
