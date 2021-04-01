using Microsoft.Azure.Cosmos.Table;
using System;

namespace BattleshipContestFunc.Data
{
    public class PlayerResult : TableEntity
    {
        public PlayerResult()
        {
            PartitionKey = Constants.PlayerResultPartitionKey;
        }

        public PlayerResult(Guid id) : this()
        {
            RowKey = id.ToString();
        }

        public string Name { get; set; } = string.Empty;

        public DateTime LastMeasurement { get; set; }

        public double AvgNumberOfShots { get; set; }
    }
}
