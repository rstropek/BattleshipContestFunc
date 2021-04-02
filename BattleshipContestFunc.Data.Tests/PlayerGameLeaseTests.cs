using Azure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BattleshipContestFunc.Data.Tests
{
    public class PlayerGameLeaseTests : IClassFixture<StorageFixture>
    {
        private readonly StorageFixture storageFixture;

        public PlayerGameLeaseTests(StorageFixture storageFixture)
        {
            this.storageFixture = storageFixture;
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task DoubleAcquire()
        {
            var playerId = Guid.NewGuid();

            var lease = await storageFixture.PlayerGameLease.Acquire(playerId);

            Assert.NotNull(lease);
            await Assert.ThrowsAsync<RequestFailedException>(async () => await storageFixture.PlayerGameLease.Acquire(playerId));

            await storageFixture.PlayerGameLease.Delete(playerId, lease);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task AcquireRenew()
        {
            var playerId = Guid.NewGuid();

            var lease = await storageFixture.PlayerGameLease.Acquire(playerId);
            await storageFixture.PlayerGameLease.Renew(playerId, lease);

            await storageFixture.PlayerGameLease.Delete(playerId, lease);
        }

        [Fact]
        [Trait("Type", "Integration")]
        public async Task AcquireReleaseAcquire()
        {
            var playerId = Guid.NewGuid();

            var lease = await storageFixture.PlayerGameLease.Acquire(playerId);
            await storageFixture.PlayerGameLease.Release(playerId, lease);
            lease = await storageFixture.PlayerGameLease.Acquire(playerId);

            await storageFixture.PlayerGameLease.Delete(playerId, lease);
        }

    }
}
