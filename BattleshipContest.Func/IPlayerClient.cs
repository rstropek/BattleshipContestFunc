using System.Threading.Tasks;

namespace BattleshipContestFunc
{
    public interface IPlayerClient
    {
        Task GetReady(string playerWebApiUrl, string? apiKey = null);
        Task<int> PlayGame(string playerWebApiUrl, string? apiKey = null);

        Task PlaySingleMoveInRandomGame(string playerWebApiUrl, string? apiKey = null);
    }
}
