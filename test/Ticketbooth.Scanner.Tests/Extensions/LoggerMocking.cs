using Microsoft.Extensions.Logging;
using Moq;
using System;

namespace Ticketbooth.Scanner.Tests.Extensions
{
    public static class LoggerMocking
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, string failMessage = null)
        {
            loggerMock.VerifyLog(level, Times.Once(), failMessage);
        }

        public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, Times times, string failMessage = null)
        {
            loggerMock.Verify(l => l.Log(level, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), times, failMessage);
        }
    }
}
