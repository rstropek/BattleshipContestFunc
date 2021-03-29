using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public interface IPlayerTable : IRepositoryTable<Player, string, Guid>
    {
    }

    public class PlayerTable : RepositoryTable<Player, string, Guid>, IPlayerTable
    {
        public PlayerTable(ILogger<RepositoryTable<Player, string, Guid>> logger, IRepository repository)
            : base(logger, repository, Constants.PlayersTable, Constants.PlayersPartitionKey)
        {
        }
    }
}
