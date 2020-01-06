using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Stratis.SmartContracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Infrastructure;
using Ticketbooth.Scanner.Tests.Extensions;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Services.Application
{
    public class TicketCheckerTests
    {
        private Mock<ILogger<TicketChecker>> _logger;
        private Mock<ITicketService> _ticketService;
        private ITicketChecker _ticketChecker;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger<TicketChecker>>();
            _ticketService = new Mock<ITicketService>();
            _ticketChecker = new TicketChecker(_logger.Object, _ticketService.Object);
        }

        [Test]
        public async Task PerformTicketCheck_CancellationRequested_LogsErrorReturnsNull()
        {
            // Arrange
            var cancellationToken = new CancellationToken(canceled: true);
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            // Act
            var result = await _ticketChecker.PerformTicketCheckAsync(ticket, cancellationToken);

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsNull_CheckReservationAsyncPolled()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var cancellationToken = cancellationTokenSource.Token;
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = null as MethodCallResponse;
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));

            // Act
            var result = await _ticketChecker.PerformTicketCheckAsync(ticket, cancellationToken);

            // Assert
            _ticketService.Verify(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address), Times.AtLeast(2));
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsSuccessFalse_LogsErrorReturnsNull()
        {
            // Arrange
            var cancellationToken = new CancellationToken(canceled: false);
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = new MethodCallResponse { Success = false, Message = "No balance" };
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));

            // Act
            var result = await _ticketChecker.PerformTicketCheckAsync(ticket, cancellationToken);

            // Assert
            _logger.VerifyLog(LogLevel.Error);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsSuccessTrue_ReturnsTransactionHash()
        {
            // Arrange
            var cancellationToken = new CancellationToken(canceled: false);
            var transactionHash = "eiw3920kdm3i_d382ualqq0_9iu99i8";
            var seat = new Seat { Number = 2, Letter = 'C' };
            var ticket = new DigitalTicket
            {
                Seat = seat,
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = new MethodCallResponse { TransactionId = transactionHash, Success = true };
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));

            // Act
            var result = await _ticketChecker.PerformTicketCheckAsync(ticket, cancellationToken);

            // Assert
            Assert.That(result, Is.EqualTo(transactionHash));
        }
    }
}
