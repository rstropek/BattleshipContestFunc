using Microsoft.Azure.Cosmos.Table;
using System;

namespace BattleshipContestFunc.Data
{
    public class GameResult : TableEntity
    {
        public GameResult() { }

        public GameResult(Guid gameId)
        {
            PartitionKey = gameId.ToString();
            RowKey = Guid.NewGuid().ToString();
        }

        public GameResult(Guid gameId, int numberOfShots) : this(gameId)
        {
            NumberOfShots = numberOfShots;
        }

        public int NumberOfShots { get; set; }
    }
}
