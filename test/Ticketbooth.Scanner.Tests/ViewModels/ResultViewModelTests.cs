using Moq;
using NUnit.Framework;
using Stratis.SmartContracts;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Services;
using Ticketbooth.Scanner.ViewModels;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class ResultViewModelTests
    {
        private Mock<ISmartContractService> _smartContractService;
        private Mock<ITicketService> _ticketService;
        private IResultViewModel _resultViewModel;

        [SetUp]
        public void SetUp()
        {
            _smartContractService = new Mock<ISmartContractService>();
            _ticketService = new Mock<ITicketService>();
            _resultViewModel = new ResultViewModel(_smartContractService.Object, _ticketService.Object);
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsNull_DetailsSetAsEmpty()
        {
            // Arrange
            var seat = new Seat { Number = 2, Letter = 'C' };
            var address = new Address(45839, 32483, 42348, 42383, 90123);

            var methodCallResponse = null as MethodCallResponse;
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(seat, address)).Returns(Task.FromResult(methodCallResponse));

            // Act
            await _resultViewModel.PerformTicketCheckAsync(seat, address, default);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<object>(It.IsAny<string>()), Times.Never);
            Assert.Multiple(() =>
            {
                Assert.That(_resultViewModel.OwnsTicket, Is.False, nameof(ResultViewModel.OwnsTicket));
                Assert.That(_resultViewModel.Name, Is.Empty, nameof(ResultViewModel.Name));
            });
        }

        [Test]
        public async Task PerformTicketCheck_ReservationCheckReturnsSuccessFalse_DetailsSetAsEmpty()
        {
            // Arrange
            var seat = new Seat { Number = 2, Letter = 'C' };
            var address = new Address(45839, 32483, 42348, 42383, 90123);

            var methodCallResponse = new MethodCallResponse { Success = false };
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(seat, address)).Returns(Task.FromResult(methodCallResponse));

            // Act
            await _resultViewModel.PerformTicketCheckAsync(seat, address, default);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<object>(It.IsAny<string>()), Times.Never);
            Assert.Multiple(() =>
            {
                Assert.That(_resultViewModel.OwnsTicket, Is.False, nameof(ResultViewModel.OwnsTicket));
                Assert.That(_resultViewModel.Name, Is.Empty, nameof(ResultViewModel.Name));
            });
        }

        [Test]
        public async Task PerformTicketCheck_FetchReceiptReturnsDefault_DetailsSetAsEmpty()
        {
            // Arrange
            var seat = new Seat { Number = 2, Letter = 'C' };
            var address = new Address(45839, 32483, 42348, 42383, 90123);

            var methodCallResponse = new MethodCallResponse { Success = true, TransactionId = "fx0je9sjeehtux" };
            var receipt = new Receipt<ReservationQueryResult>
            {
                ReturnValue = default
            };
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(seat, address)).Returns(Task.FromResult(methodCallResponse));
            _smartContractService.Setup(callTo => callTo.FetchReceiptAsync<ReservationQueryResult>("fx0je9sjeehtux")).Returns(Task.FromResult(receipt));

            // Act
            await _resultViewModel.PerformTicketCheckAsync(seat, address, default);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<object>(It.IsAny<string>()), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(_resultViewModel.OwnsTicket, Is.False, nameof(ResultViewModel.OwnsTicket));
                Assert.That(_resultViewModel.Name, Is.Empty, nameof(ResultViewModel.Name));
            });
        }

        [Test]
        public async Task PerformTicketCheck_FetchReceiptReturnsValues_DetailsSetCorrectly()
        {
            // Arrange
            var seat = new Seat { Number = 2, Letter = 'C' };
            var address = new Address(45839, 32483, 42348, 42383, 90123);

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
            _ticketService.Setup(callTo => callTo.CheckReservationAsync(seat, address)).Returns(Task.FromResult(methodCallResponse));
            _smartContractService.Setup(callTo => callTo.FetchReceiptAsync<ReservationQueryResult>("fx0je9sjeehtux")).Returns(Task.FromResult(receipt));

            // Act
            await _resultViewModel.PerformTicketCheckAsync(seat, address, default);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<object>(It.IsAny<string>()), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(_resultViewModel.OwnsTicket, Is.EqualTo(reservationQueryResult.OwnsTicket), nameof(ResultViewModel.OwnsTicket));
                Assert.That(_resultViewModel.Name, Is.EqualTo(reservationQueryResult.CustomerIdentifier), nameof(ResultViewModel.Name));
            });
        }
    }
}
