using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Data.Tests
{
    public class PlayerResultTableTests : IClassFixture<RepositoryTableFixture>
    {
        private readonly RepositoryTableFixture tableFixture;

        public PlayerResultTableTests(RepositoryTableFixture tableFixture)
        {
            this.tableFixture = tableFixture;
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task AddOrUpdate_Update()
        {
            var prt = new PlayerResultTable(Mock.Of<ILogger<RepositoryTable<PlayerResult, string, Guid>>>(), tableFixture.Repository);

            var playerId = Guid.NewGuid();
            var result = new PlayerResult(playerId) { Name = "dummy", LastMeasurement = DateTime.UtcNow, AvgNumberOfShots = 1d };
            await prt.Add(result);

            await prt.AddOrUpdate(playerId, "FooBar", DateTime.UtcNow, 2d);

            Assert.Equal("FooBar", (await prt.GetSingle(playerId))!.Name);

            await prt.Delete(playerId);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task AddOrUpdate_Add()
        {
            var prt = new PlayerResultTable(Mock.Of<ILogger<RepositoryTable<PlayerResult, string, Guid>>>(), tableFixture.Repository);

            var playerId = Guid.NewGuid();
            await prt.AddOrUpdate(playerId, "FooBar", DateTime.UtcNow, 2d);

            Assert.Equal("FooBar", (await prt.GetSingle(playerId))!.Name);

            await prt.Delete(playerId);
        }
    }
}