using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public interface IGameResultTable : IRepositoryTable<GameResult, Guid, Guid>
    {
    }

    public class GameResultTable : RepositoryTable<GameResult, Guid, Guid>, IGameResultTable
    {
        public GameResultTable(ILogger<RepositoryTable<GameResult, Guid, Guid>> logger, IRepository repository)
            : base(logger, repository, Constants.GameResultTable)
        {
        }

        public async Task RemoveAllResults(Guid gameId)
        {
            var entries = await Get(r => r.PartitionKey == gameId.ToString());
            foreach(var entry in entries)
            {

            }
        }
    }
}
