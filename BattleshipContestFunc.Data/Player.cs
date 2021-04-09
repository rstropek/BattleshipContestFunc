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

        public Guid GetPlayerIdGuid() => Guid.Parse(RowKey);

        public string Name { get; set; } = string.Empty;

        public string WebApiUrl { get; set; } = string.Empty;

        public string? ApiKey { get; set; }

        public string Creator { get; set; } = string.Empty;

        public string? GitHubUrl { get; set; }

        public DateTime? TournamentInProgressSince { get; set; }

        public bool? NeedsThrottling { get; set; }
    }
}
