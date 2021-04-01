using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public interface IPlayerLogTable : IRepositoryTable<PlayerLog, Guid, string>
    {
        Task AddException(string playerIdString, string playerUrl, string ex);
        Task AddException(Guid playerId, string playerUrl, string ex);
    }

    public class PlayerLogTable : RepositoryTable<PlayerLog, Guid, string>, IPlayerLogTable
    {
        public PlayerLogTable(ILogger<RepositoryTable<PlayerLog, Guid, string>> logger, IRepository repository)
            : base(logger, repository, Constants.PlayerLogTable)
        {
        }

        public async Task AddException(Guid playerId, string playerUrl, string ex)
            => await AddException(playerId.ToString(), playerUrl, ex);

        public async Task AddException(string playerIdString, string playerUrl, string ex)
        {
            await Add(new(playerIdString, playerUrl, ex));
        }
    }
}
