using Microsoft.Azure.Cosmos.Table;
using System;

namespace BattleshipContestFunc.Data
{
    public class Contest : TableEntity
    {
        public Contest() { }

        public Contest(string contestName)
            : this(Guid.NewGuid(), contestName) { }

        public Contest(Guid contestId, string contestName)
        {
            if (string.IsNullOrEmpty(contestName))
            {
                throw new ArgumentOutOfRangeException(nameof(contestName));
            }

            PartitionKey = Constants.ContestsPartitionKey;
            ContestId = contestId;
            RowKey = contestId.ToString();
        }

        public Guid ContestId { get; } = Guid.Empty;

        public string ContestName { get; } = string.Empty;
    }
}
