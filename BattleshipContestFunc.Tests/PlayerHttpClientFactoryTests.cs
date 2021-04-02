using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class PlayerHttpClientFactoryTests
    {
        [Fact]
        public void GetHttpClient()
        {
            IPlayerHttpClientFactory factory = new PlayerHttpClientFactory();

            var client1 = factory.GetHttpClient("http://someapi.com") as PlayerHttpClient;
            Assert.NotNull(client1);
            Assert.Equal("http://someapi.com/", client1!.client.BaseAddress!.AbsoluteUri);

            var client2 = factory.GetHttpClient("http://someapi.com/") as PlayerHttpClient;
            Assert.Equal(client1, client2);
        }
    }
}
