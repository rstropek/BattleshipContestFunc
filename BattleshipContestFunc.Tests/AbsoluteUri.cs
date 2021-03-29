using Xunit;

namespace BattleshipContestFunc.Tests
{
    public class AbsoluteUri
    {
        [Fact]
        public void NullIsValid()
        {
            var attr = new AbsoluteUriAttribute();
            Assert.True(attr.IsValid(null));
        }

        [Fact]
        public void Invalid()
        {
            var attr = new AbsoluteUriAttribute();
            Assert.False(attr.IsValid("ab/cd"));
        }

        [Fact]
        public void Valid()
        {
            var attr = new AbsoluteUriAttribute();
            Assert.True(attr.IsValid("https://myserver.com/ab/cd"));
        }
    }
}
