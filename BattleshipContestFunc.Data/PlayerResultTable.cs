using Microsoft.Extensions.Logging;
using System;

namespace BattleshipContestFunc.Data
{
    public interface IPlayerResultTable : IRepositoryTable<PlayerResult, string, Guid>
    {
    }

    public class PlayerResultTable : RepositoryTable<PlayerResult, string, Guid>, IPlayerResultTable
    {
        public PlayerResultTable(ILogger<RepositoryTable<PlayerResult, string, Guid>> logger, IRepository repository)
            : base(logger, repository, Constants.PlayerResultTable, Constants.PlayerResultPartitionKey)
        {
        }
    }
}
