using System;

namespace BattleshipContestFunc.Data.Tests
{
    public static class RandomTableName
    {
        public static string Generate(int nameLength = 20)
            => string.Create(nameLength, new Random(), (buf, rand) =>
            {
                for (var i = 0; i < nameLength; i++)
                {
                    buf[i] = (char)('a' + rand.Next(25));
                }
            });
    }
}
