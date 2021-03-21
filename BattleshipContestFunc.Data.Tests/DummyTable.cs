using Microsoft.Azure.Cosmos.Table;
using System;

namespace BattleshipContestFunc.Data
{
    public class DummyTable : TableEntity
    {
        public DummyTable() { }

        public DummyTable(string dummyName)
            : this(Guid.NewGuid(), dummyName) { }

        public DummyTable(Guid dummyId, string dummyName)
        {
            PartitionKey = nameof(DummyTable);
            DummyId = dummyId;
            RowKey = dummyId.ToString();
            DummyName = dummyName;
        }

        public Guid DummyId { get; set; } = Guid.Empty;

        public string DummyName { get; set; } = string.Empty;
    }
}
