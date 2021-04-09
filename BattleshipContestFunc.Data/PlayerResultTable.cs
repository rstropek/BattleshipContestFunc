using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BattleshipContestFunc.Data
{
    public interface IPlayerResultTable : IRepositoryTable<PlayerResult, string, Guid>
    {
        Task AddOrUpdate(Guid playerId, string playerName, DateTime lastMeasurement, double avgShots);
    }

    public class PlayerResultTable : RepositoryTable<PlayerResult, string, Guid>, IPlayerResultTable
    {
        public PlayerResultTable(ILogger<RepositoryTable<PlayerResult, string, Guid>> logger, IRepository repository)
            : base(logger, repository, Constants.PlayerResultTable, Constants.PlayerResultPartitionKey)
        {
        }

        public async Task AddOrUpdate(Guid playerId, string playerName, DateTime lastMeasurement, double avgShots)
        {
            var insert = false;
            var playerResultEntry = await GetSingle(playerId);
            if (playerResultEntry == null)
            {
                playerResultEntry = new(playerId) { Name = playerName };
                insert = true;
            }

            playerResultEntry.LastMeasurement = lastMeasurement;
            playerResultEntry.AvgNumberOfShots = avgShots;
            playerResultEntry.Name = playerName;
            if (insert) await Add(playerResultEntry);
            else await Replace(playerResultEntry);
        }
    }
}
