using Moq;
using NUnit.Framework;
using System.Linq;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.ViewModels;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.ViewModels
{
    public class AppStateViewModelTests
    {
        private Mock<ITicketChecker> _ticketChecker;
        private AppStateViewModel _appStateViewModel;

        [SetUp]
        public void SetUp()
        {
            _ticketChecker = new Mock<ITicketChecker>();
            _appStateViewModel = new AppStateViewModel(_ticketChecker.Object);
        }

        [Test]
        public void AddTicketScan_TicketScanned_AddedToList()
        {
            // Arrange
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ticketCheckEventArgs = new TicketCheckEventArgs(transactionHash, seat);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_appStateViewModel.TicketScans.Last(),
                    Has.Property(nameof(TicketScanModel.TransactionHash)).EqualTo(transactionHash),
                    nameof(TicketScanModel.TransactionHash));
                Assert.That(_appStateViewModel.TicketScans.Last(),
                    Has.Property(nameof(TicketScanModel.Seat)).Property(nameof(SeatModel.Number)).EqualTo(seat.Number),
                    "Seat number");
                Assert.That(_appStateViewModel.TicketScans.Last(),
                    Has.Property(nameof(TicketScanModel.Seat)).Property(nameof(SeatModel.Letter)).EqualTo(seat.Letter),
                    "Seat letter");
            });
        }

        [Test]
        public void AddTicketScan_TicketScanned_OnDataChangedRaised()
        {
            // Arrange
            var eventRaised = false;
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ticketCheckEventArgs = new TicketCheckEventArgs(transactionHash, seat);
            _appStateViewModel.OnDataChanged += (s, e) => { eventRaised = true; };

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Assert
            Assert.That(eventRaised, Is.True);

            _appStateViewModel.OnDataChanged -= (s, e) => { eventRaised = true; };
        }
    }
}
