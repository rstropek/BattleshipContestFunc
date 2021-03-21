using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace BattleshipContestFunc.Data.Tests
{
    public static class LogVerificationExtensions
    {
        public static Mock<ILogger<T>> VerifyLogWasCalled<T>(this Mock<ILogger<T>> logger, LogLevel level, Times times)
        {
            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == level),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), times);

            return logger;
        }
    }
}
