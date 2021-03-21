using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Data.Tests
{
    public class RepositoryTests : IClassFixture<RepositoryFixture>
    {
        private readonly RepositoryFixture repoFixture;

        public RepositoryTests(RepositoryFixture repoFixture)
        {
            this.repoFixture = repoFixture;
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task EnsureTableCreatedDeleted()
        {
            var tableName = RandomTableName.Generate();

            await repoFixture.Repository.EnsureTableCreated(tableName);
            Assert.NotNull(await repoFixture.Repository.GetTable(tableName));

            await repoFixture.Repository.EnsureTableDeleted(tableName);
            Assert.Null(await repoFixture.Repository.GetTable(tableName));
        }
    }
}
