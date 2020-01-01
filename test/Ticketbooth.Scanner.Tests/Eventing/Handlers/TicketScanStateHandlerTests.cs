using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Eventing.Handlers;
using Ticketbooth.Scanner.Services.Application;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Eventing.Handlers
{
    public class TicketScanStateHandlerTests
    {
        private List<TicketScanModel> _fakeTicketScans;

        private Mock<ITicketChecker> _ticketChecker;
        private Mock<ITicketRepository> _ticketRepository;

        [SetUp]
        public void SetUp()
        {
            _ticketChecker = new Mock<ITicketChecker>();
            _ticketRepository = new Mock<ITicketRepository>();

            _fakeTicketScans = new List<TicketScanModel>();
            _ticketRepository.Setup(callTo => callTo.Find(It.IsAny<string>()))
                .Returns<string>(key => _fakeTicketScans.FirstOrDefault(scan => scan.TransactionHash.Equals(key)));
            _ticketRepository.Setup(callTo => callTo.Add(It.IsAny<TicketScanModel>()))
                .Callback(new Action<TicketScanModel>(ticketScan => _fakeTicketScans.Add(ticketScan)));

            new TicketScanStateHandler(_ticketChecker.Object, _ticketRepository.Object);
        }

        [Test]
        public void AddTicketScan_OnCheckTicket_AddedToList()
        {
            // Arrange
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ticketCheckEventArgs = new TicketCheckRequestEventArgs(transactionHash, seat);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_fakeTicketScans.Last(),
                    Has.Property(nameof(TicketScanModel.TransactionHash)).EqualTo(transactionHash),
                    nameof(TicketScanModel.TransactionHash));
                Assert.That(_fakeTicketScans.Last(),
                    Has.Property(nameof(TicketScanModel.Seat)).Property(nameof(SeatModel.Number)).EqualTo(seat.Number),
                    "Seat number");
                Assert.That(_fakeTicketScans.Last(),
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
            var ticketCheckEventArgs = new TicketCheckRequestEventArgs(transactionHash, seat);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Assert
            Assert.That(_fakeTicketScans.Last(),
                Has.Property(nameof(TicketScanModel.Status)).EqualTo(TicketScanStatus.Started));
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
            Assert.That(_fakeTicketScans.Count(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)), Is.Zero);
        }

        [Test]
        public void SetTicketScanResult_OnCheckTicketResultTxExists_TicketUpdated()
        {
            // Arrange
            var transactionHash = "84jfw89j";
            var seat = new Seat { Number = 5, Letter = 'B' };
            var ownsTicket = true;
            var name = "Benjamin Swift";
            var ticketCheckEventArgs = new TicketCheckRequestEventArgs(transactionHash, seat);
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, ownsTicket, name);
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(_fakeTicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)),
                    Has.Property(nameof(TicketScanModel.OwnsTicket)).EqualTo(ownsTicket),
                    nameof(TicketScanModel.OwnsTicket));
                Assert.That(_fakeTicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)),
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
            var ticketCheckEventArgs = new TicketCheckRequestEventArgs(transactionHash, seat);
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, ownsTicket, name, false);
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.That(_fakeTicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)),
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
            var ticketCheckEventArgs = new TicketCheckRequestEventArgs(transactionHash, seat);
            var ticketCheckResultEventArgs = new TicketCheckResultEventArgs(transactionHash, ownsTicket, name, true);
            _ticketChecker.Raise(callTo => callTo.OnCheckTicket += null, ticketCheckEventArgs);

            // Act
            _ticketChecker.Raise(callTo => callTo.OnCheckTicketResult += null, ticketCheckResultEventArgs);

            // Assert
            Assert.That(_fakeTicketScans.FirstOrDefault(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)),
                Has.Property(nameof(TicketScanModel.Status)).EqualTo(TicketScanStatus.Faulted));
        }
    }
}
