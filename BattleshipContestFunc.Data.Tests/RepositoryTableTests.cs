using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Data.Tests
{
    public class RepositoryTableTests : IClassFixture<RepositoryTableFixture>
    {
        private readonly RepositoryTableFixture tableFixture;

        public RepositoryTableTests(RepositoryTableFixture tableFixture)
        {
            this.tableFixture = tableFixture;
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task AddWrongPartitionKey()
        {
            var table = new DummyRepositoryTable(tableFixture, true);
            await Assert.ThrowsAsync<InvalidPartitionKeyException>(() => table.Table.Add(new()));
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task Add()
        {
            var dummyId = Guid.NewGuid();
            const string dummyName = "Dummy";

            var table = new DummyRepositoryTable(tableFixture, true);
            await table.Table.Add(new(dummyId, dummyName));
            var item = await table.Table.GetSingle(dummyId);

            Assert.NotNull(item);
            Assert.Equal(dummyId, item!.DummyId);
            Assert.Equal(dummyId.ToString(), item!.RowKey);
            Assert.Equal(dummyName, item!.DummyName);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task GetMissingPartitionKey()
        {
            var table = new DummyRepositoryTable(tableFixture);
            await Assert.ThrowsAsync<InvalidPartitionKeyException>(() => table.Table.Get());
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task Get()
        {
            var dummyId = Guid.NewGuid();
            const string dummyName = "Dummy";

            var table = new DummyRepositoryTable(tableFixture, true);
            await table.Table.Add(new(dummyId, dummyName));
            await table.Table.Add(new(Guid.NewGuid(), dummyName));

            var result = await table.Table.Get(item => item.DummyId == dummyId);

            Assert.Single(result);
            Assert.Equal(dummyName, result.First().DummyName);
            Assert.Equal(dummyId, result.First().DummyId);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task GetLoggingPartitionKey()
        {
            var table = new DummyRepositoryTable(tableFixture, true);
            await table.Table.Get("SomeDummyPartitionKey");
            table.LoggerMock.VerifyLogWasCalled(LogLevel.Warning, Times.Once());
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task GetSingleMissingPartitionKey()
        {
            var table = new DummyRepositoryTable(tableFixture);
            await Assert.ThrowsAsync<InvalidPartitionKeyException>(() => table.Table.GetSingle(Guid.NewGuid()));
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task GetSingle()
        {
            var dummyId = Guid.NewGuid();
            const string dummyName = "Dummy";

            var table = new DummyRepositoryTable(tableFixture, true);
            await table.Table.Add(new(dummyId, dummyName));

            var result = await table.Table.GetSingle(dummyId);

            Assert.NotNull(result);
            Assert.Equal(dummyName, result!.DummyName);
            Assert.Equal(dummyId, result.DummyId);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task GetSingleLoggingPartitionKey()
        {
            var table = new DummyRepositoryTable(tableFixture, true);
            await table.Table.GetSingle("SomeDummyPartitionKey", Guid.NewGuid());
            table.LoggerMock.VerifyLogWasCalled(LogLevel.Warning, Times.Once());
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task DeleteMissingPartitionKey()
        {
            var table = new DummyRepositoryTable(tableFixture);
            await Assert.ThrowsAsync<InvalidPartitionKeyException>(() => table.Table.Delete(Guid.NewGuid()));
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task Delete()
        {
            var dummyId = Guid.NewGuid();
            const string dummyName = "Dummy";

            var table = new DummyRepositoryTable(tableFixture, true);
            await table.Table.Add(new(dummyId, dummyName));

            await table.Table.Delete(dummyId);

            var result = await table.Table.GetSingle(dummyId);
            Assert.Null(result);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task DeleteEntity()
        {
            var dummyId = Guid.NewGuid();
            const string dummyName = "Dummy";

            var table = new DummyRepositoryTable(tableFixture, true);
            await table.Table.Add(new(dummyId, dummyName));

            var entity = await table.Table.GetSingle(dummyId);
            await table.Table.Delete(entity!);

            var result = await table.Table.GetSingle(dummyId);
            Assert.Null(result);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task DeleteLoggingPartitionKey()
        {
            var table = new DummyRepositoryTable(tableFixture, true);
            await table.Table.Delete("SomeDummyPartitionKey", Guid.NewGuid());
            table.LoggerMock.VerifyLogWasCalled(LogLevel.Warning, Times.Once());
        }
    }
}