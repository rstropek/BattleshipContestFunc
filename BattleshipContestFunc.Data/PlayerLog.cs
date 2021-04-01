using Microsoft.Azure.Cosmos.Table;
using System;

namespace BattleshipContestFunc.Data
{
    public class PlayerLog : TableEntity
    {
        public PlayerLog() { }

        public PlayerLog(Guid playerId) : this(playerId, DateTime.Now)
        {
        }

        public PlayerLog(Guid playerId, string logMessage) : this(playerId, DateTime.Now)
        {
            LogMessage = logMessage;
        }
        public PlayerLog(Guid playerId, string webApiUrl, string logMessage) : this(playerId, logMessage)
        {
            WebApiUrl = webApiUrl;
        }

        public PlayerLog(string playerId) : this(Guid.Parse(playerId))
        {
        }
        public PlayerLog(string playerId, string logMessage) : this(Guid.Parse(playerId), logMessage)
        {
        }
        public PlayerLog(string playerId, string webApiUrl, string logMessage) : this(playerId, logMessage)
        {
            WebApiUrl = webApiUrl;
        }

        public PlayerLog(Guid playerId, DateTime logTime) : this()
        {
            PartitionKey = playerId.ToString();
            RowKey = $"{logTime.ToUniversalTime():u}-{Guid.NewGuid()}";
        }

        public string LogMessage { get; set; } = string.Empty;

        public string WebApiUrl { get; set; } = string.Empty;
    }
}
