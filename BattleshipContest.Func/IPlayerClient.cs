using NBattleshipCodingContest.Logic;
using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IPlayerClient
    {
        Task GetReady(string playerWebApiUrl, string? apiKey = null);

        Task<BoardIndex> GetShot(string playerWebApiUrl, ISinglePlayerGame game, string? apiKey = null);
    }
}
