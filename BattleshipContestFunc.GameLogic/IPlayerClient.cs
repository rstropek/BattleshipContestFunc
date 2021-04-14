using NBattleshipCodingContest.Logic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IPlayerClient
    {
        Task GetReady(string playerWebApiUrl, int numberOfGames, string? apiKey = null);

        Task<BoardIndex> GetShot(string playerWebApiUrl, ISinglePlayerGame game, string? apiKey = null);

        Task<IReadOnlyList<BoardIndex>> GetShots(string playerWebApiUrl, IEnumerable<ISinglePlayerGame> games, string? apiKey = null);

        Task Finished(string playerWebApiUrl, IEnumerable<ISinglePlayerGame> games, string? apiKey = null);
    }
}
