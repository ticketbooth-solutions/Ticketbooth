using Moq;
using NUnit.Framework;
using Stratis.SmartContracts;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Services.Application;
using Ticketbooth.Scanner.Services.Infrastructure;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Services.Application
{
    public class TicketCheckerTests
    {
        private Mock<ITicketService> _ticketService;
        private ITicketChecker _ticketChecker;

        [SetUp]
        public void SetUp()
        {
            _ticketService = new Mock<ITicketService>();
            _ticketChecker = new TicketChecker(_ticketService.Object);
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsNull_ReturnsNull()
        {
            // Arrange
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = null as MethodCallResponse;
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));

            // Act
            var result = await _ticketChecker.PerformTicketCheckAsync(ticket);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsSuccessFalse_ReturnsNull()
        {
            // Arrange
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = new MethodCallResponse { Success = false };
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));

            // Act
            var result = await _ticketChecker.PerformTicketCheckAsync(ticket);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsSuccessTrue_ReturnsTransactionHash()
        {
            // Arrange
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
            var result = await _ticketChecker.PerformTicketCheckAsync(ticket);

            // Assert
            Assert.That(result, Is.EqualTo(transactionHash));
        }
    }
}
