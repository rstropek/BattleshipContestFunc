using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace BattleshipContestFunc.Data.Tests
{
    public class DummyRepositoryTable
    {
        public DummyRepositoryTable(RepositoryTableFixture fixture, bool withPartitionKey = false)
        {
            LoggerMock = new Mock<ILogger<RepositoryTable<DummyTable, string, Guid>>>();
            var logger = LoggerMock.Object;

            Table = withPartitionKey
                ? new RepositoryTable<DummyTable, string, Guid>(logger, fixture.Repository, fixture.TableName, nameof(DummyTable))
                : new RepositoryTable<DummyTable, string, Guid>(logger, fixture.Repository, fixture.TableName);
        }

        public RepositoryTable<DummyTable, string, Guid> Table { get; }
        public Mock<ILogger<RepositoryTable<DummyTable, string, Guid>>> LoggerMock { get; }
    }
}
