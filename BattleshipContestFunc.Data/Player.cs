using Microsoft.Azure.Cosmos.Table;
using System;

namespace BattleshipContestFunc.Data
{
    public class Player : TableEntity
    {
        public Player()
        {
            PartitionKey = Constants.PlayersPartitionKey;
        }

        public Player(Guid id) : this()
        {
            RowKey = id.ToString();
        }

        public string Name { get; set; } = string.Empty;

        public string WebApiUrl { get; set; } = string.Empty;
    }
}
