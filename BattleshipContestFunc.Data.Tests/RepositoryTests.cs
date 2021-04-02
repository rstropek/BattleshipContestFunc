using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Data.Tests
{
    public class RepositoryTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture storageFixture;

        public RepositoryTests(StorageFixture storageFixture)
        {
            this.storageFixture = storageFixture;
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task EnsureTableCreatedDeleted()
        {
            var tableName = RandomTableName.Generate();

            await storageFixture.Repository.EnsureTableCreated(tableName);
            Assert.NotNull(await storageFixture.Repository.GetTable(tableName));

            await storageFixture.Repository.EnsureTableDeleted(tableName);
            Assert.Null(await storageFixture.Repository.GetTable(tableName));
        }
    }
}
