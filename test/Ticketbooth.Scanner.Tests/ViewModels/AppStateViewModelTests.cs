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
        public void AddTicketScan_OnCheckTicket_AddedToList()
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
        public void AddTicketScan_OnCheckTicket_TicketScanStatusIsStarted()
        {
            // Arrange
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ticketCheckEventArgs = new TicketCheckEventArgs(transactionHash, seat);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Assert
            Assert.That(_appStateViewModel.TicketScans.Last(),
                Has.Property(nameof(TicketScanModel.Status)).EqualTo(TicketScanStatus.Started));
        }

        [Test]
        public void AddTicketScan_OnCheckTicket_OnDataChangedRaised()
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

        [Test]
        public void SetTicketScanResult_OnCheckTicketResultTxDoesNotExist_TicketDoesNotExist()
        {
            // Arrange
            var transactionHash = "84jfw89j";
            var ownsTicket = true;
            var name = "Benjamin Swift";
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, ownsTicket, name);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.That(_appStateViewModel.TicketScans.Count(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)), Is.Zero);
        }

        [Test]
        public void SetTicketScanResult_OnCheckTicketResultTxExists_TicketUpdated()
        {
            // Arrange
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ownsTicket = true;
            var name = "Benjamin Swift";
            var ticketCheckEventArgs = new TicketCheckEventArgs(transactionHash, seat);
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, ownsTicket, name);
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_appStateViewModel.TicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)),
                    Has.Property(nameof(TicketScanModel.OwnsTicket)).EqualTo(ownsTicket),
                    nameof(TicketScanModel.OwnsTicket));
                Assert.That(_appStateViewModel.TicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)),
                    Has.Property(nameof(TicketScanModel.Name)).EqualTo(name),
                    nameof(TicketScanModel.Name));
            });
        }

        [Test]
        public void SetTicketScanResult_OnCheckTicketResultTxExistsNotFaulted_TicketScanStatusIsCompleted()
        {
            // Arrange
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ownsTicket = true;
            var name = "Benjamin Swift";
            var ticketCheckEventArgs = new TicketCheckEventArgs(transactionHash, seat);
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, ownsTicket, name, false);
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.That(_appStateViewModel.TicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)),
                Has.Property(nameof(TicketScanModel.Status)).EqualTo(TicketScanStatus.Completed));
        }

        [Test]
        public void SetTicketScanResult_OnCheckTicketResultTxExistsButFaulted_TicketScanStatusIsFaulted()
        {
            // Arrange
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ownsTicket = true;
            var name = "Benjamin Swift";
            var ticketCheckEventArgs = new TicketCheckEventArgs(transactionHash, seat);
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, ownsTicket, name, true);
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.That(_appStateViewModel.TicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)),
                Has.Property(nameof(TicketScanModel.Status)).EqualTo(TicketScanStatus.Faulted));
        }

        [Test]
        public void SetTicketScanResult_OnCheckTicketResultTxDoesNotExist_OnDataChangedNotRaised()
        {
            // Arrange
            var eventRaised = false;
            var transactionHash = "84jfw89j";
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, true, "Benjamin Swift");

            _appStateViewModel.OnDataChanged += (s, e) => { eventRaised = true; };

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.That(eventRaised, Is.False);

            _appStateViewModel.OnDataChanged -= (s, e) => { eventRaised = true; };
        }

        [Test]
        public void SetTicketScanResult_OnCheckTicketResultTxExists_OnDataChangedRaised()
        {
            // Arrange
            var eventRaised = false;
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ticketCheckEventArgs = new TicketCheckEventArgs(transactionHash, seat);
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, true, "Benjamin Swift");
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            _appStateViewModel.OnDataChanged += (s, e) => { eventRaised = true; };

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.That(eventRaised, Is.True);

            _appStateViewModel.OnDataChanged -= (s, e) => { eventRaised = true; };
        }
    }
}
