using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IGameClient 
    {
        Task GetReadyForGame(string playerWebApiUrl, string? apiKey = null);

        Task<int> PlayGame(string playerWebApiUrl, Func<Task>? postRoundCallback = null, string? apiKey = null);

        Task PlaySingleMoveInRandomGame(string playerWebApiUrl, string? apiKey = null);
    }
}
