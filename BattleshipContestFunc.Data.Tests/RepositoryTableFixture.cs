using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Data.Tests
{
    public class RepositoryTableFixture : RepositoryFixture, IAsyncLifetime
    {
        public RepositoryTableFixture()
        {
            TableName = RandomTableName.Generate();
        }

        public string TableName { get; }

        public async Task DisposeAsync()
        {
            await Repository.EnsureTableDeleted(TableName);
        }

        public async Task InitializeAsync()
        {
            await Repository.EnsureTableCreated(TableName);
        }
    }
}
