using BattleshipContestFunc.Data;
using System;
using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class MappingTests : IClassFixture<ApiConfigFixture>
    {
        private readonly ApiConfigFixture fixture;

        public MappingTests(ApiConfigFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void PlayerAddDtoToPlayerThrottlingDefault()
        {
            var addDto = new PlayerAddDto(Guid.Empty, "FooBar", "https://somewhere.com", NeedsThrottling: null);
            var player = fixture.Mapper.Map<Player>(addDto);
            Assert.False(player.NeedsThrottling);
        }

        [Fact]
        public void PlayerToGetDtoThrottlingDefault()
        {
            var player = new Player() { NeedsThrottling = false };
            var getDto = fixture.Mapper.Map<PlayerGetDto>(player);
            Assert.False(getDto.NeedsThrottling);
        }

        [Fact]
        public void PlayerToAddDtoThrottlingDefault()
        {
            var player = new Player() { NeedsThrottling = false };
            var addDto = fixture.Mapper.Map<PlayerAddDto>(player);
            Assert.False(addDto.NeedsThrottling);
        }
    }
}
