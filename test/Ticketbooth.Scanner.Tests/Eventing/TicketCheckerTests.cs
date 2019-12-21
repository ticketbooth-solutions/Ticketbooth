using Moq;
using NUnit.Framework;
using Stratis.SmartContracts;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Eventing
{
    public class TicketCheckerTests
    {
        private Mock<ISmartContractService> _smartContractService;
        private Mock<ITicketService> _ticketService;
        private ITicketChecker _ticketChecker;

        [SetUp]
        public void SetUp()
        {
            _smartContractService = new Mock<ISmartContractService>();
            _ticketService = new Mock<ITicketService>();
            _ticketChecker = new TicketChecker(_smartContractService.Object, _ticketService.Object);
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsNull_DetailsSetAsEmpty()
        {
            // Arrange
            var eventInvoked = false;
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = null as MethodCallResponse;
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));
            _ticketChecker.OnCheckTicket += (object sender, TicketCheckEventArgs e) =>
            {
                eventInvoked = true;
                Assert.That(e.Seat, Is.EqualTo(ticket.Seat), nameof(TicketCheckEventArgs.Seat));
            };

            // Act
            var ticketCheckMade = await _ticketChecker.PerformTicketCheckAsync(ticket, default);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<object>(It.IsAny<string>()), Times.Never);
            Assert.That(eventInvoked, Is.False, "Event invoked");
            Assert.That(ticketCheckMade, Is.False, "Ticket check made");
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsSuccessFalse_DetailsSetAsEmpty()
        {
            // Arrange
            var eventInvoked = false;
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = new MethodCallResponse { Success = false };
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));
            _ticketChecker.OnCheckTicket += (object sender, TicketCheckEventArgs e) =>
            {
                eventInvoked = true;
                Assert.That(e.Seat, Is.EqualTo(ticket.Seat), nameof(TicketCheckEventArgs.Seat));
            };

            // Act
            var ticketCheckMade = await _ticketChecker.PerformTicketCheckAsync(ticket, default);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<object>(It.IsAny<string>()), Times.Never);
            Assert.That(eventInvoked, Is.False, "Event invoked");
            Assert.That(ticketCheckMade, Is.False, "Ticket check made");
        }

        [Test]
        public async Task PerformTicketCheck_FetchReceiptReturnsDefault_DetailsSetAsEmpty()
        {
            // Arrange
            var eventInvoked = false;
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = new MethodCallResponse { Success = true, TransactionId = "fx0je9sjeehtux" };
            var receipt = new Receipt<ReservationQueryResult>
            {
                ReturnValue = default
            };
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));
            _smartContractService.Setup(callTo => callTo.FetchReceiptAsync<ReservationQueryResult>("fx0je9sjeehtux")).Returns(Task.FromResult(receipt));
            _ticketChecker.OnCheckTicket += (object sender, TicketCheckEventArgs e) =>
            {
                eventInvoked = true;
                Assert.That(e.Seat, Is.EqualTo(ticket.Seat), nameof(TicketCheckEventArgs.Seat));
            };

            // Act
            var ticketCheckMade = await _ticketChecker.PerformTicketCheckAsync(ticket, default);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<object>(It.IsAny<string>()), Times.Once);
            Assert.That(eventInvoked, Is.True, "Event invoked");
            Assert.That(ticketCheckMade, Is.True, "Ticket check made");
        }

        [Test]
        public async Task PerformTicketCheck_FetchReceiptReturnsValues_DetailsSetCorrectly()
        {
            // Arrange
            var eventInvoked = false;
            var ticket = new DigitalTicket
            {
                Seat = new Seat { Number = 2, Letter = 'C' },
                Address = new Address(45839, 32483, 42348, 42383, 90123)
            };

            var methodCallResponse = new MethodCallResponse { Success = true, TransactionId = "fx0je9sjeehtux" };
            var reservationQueryResult = new ReservationQueryResult
            {
                OwnsTicket = true,
                CustomerIdentifier = "John Hammond"
            };
            var receipt = new Receipt<ReservationQueryResult>
            {
                ReturnValue = reservationQueryResult
            };
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(ticket.Seat, ticket.Address)).Returns(Task.FromResult(methodCallResponse));
            _smartContractService.Setup(callTo => callTo.FetchReceiptAsync<ReservationQueryResult>("fx0je9sjeehtux")).Returns(Task.FromResult(receipt));
            _ticketChecker.OnCheckTicket += (object sender, TicketCheckEventArgs e) =>
            {
                eventInvoked = true;
                Assert.That(e.Seat, Is.EqualTo(ticket.Seat), nameof(TicketCheckEventArgs.Seat));
            };

            // Act
            var ticketCheckMade = await _ticketChecker.PerformTicketCheckAsync(ticket, default);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<object>(It.IsAny<string>()), Times.Once);
            Assert.That(eventInvoked, Is.True, "Event invoked");
            Assert.That(ticketCheckMade, Is.True, "Ticket check made");
        }
    }
}
