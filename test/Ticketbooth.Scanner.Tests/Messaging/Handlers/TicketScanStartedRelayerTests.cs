using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing.Events;
using Ticketbooth.Scanner.Messaging.Handlers;
using Ticketbooth.Scanner.Messaging.Notifications;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Messaging.Handlers
{
    public class TicketScanStartedRelayerTests
    {
        private Mock<IMessageHub> _eventAggregator;
        private Mock<ITicketRepository> _ticketRepository;
        private TicketScanStartedRelayer _ticketScanRelayer;

        [SetUp]
        public void SetUp()
        {
            _eventAggregator = new Mock<IMessageHub>();
            _ticketRepository = new Mock<ITicketRepository>();
            _ticketScanRelayer = new TicketScanStartedRelayer(Mock.Of<ILogger<TicketScanStartedRelayer>>(), _eventAggregator.Object, _ticketRepository.Object);
        }

        [Test]
        public async Task Handle_TicketScanStartedNotification_AddedToRepository()
        {
            // Arrange
            var transactionHash = "d389jqwjfiok4mtktnwl_d3uifn3";
            var seat = new Seat { Number = 2, Letter = 'D' };
            var notification = new TicketScanStartedNotification(transactionHash, seat);

            // Act
            await _ticketScanRelayer.Handle(notification, default);

            // Assert
            _ticketRepository.Verify(
                callTo => callTo.Add(It.Is<TicketScanModel>(ticketScan => ticketScan.TransactionHash.Equals(transactionHash)
                    && ticketScan.Seat.Number == seat.Number && ticketScan.Seat.Letter == seat.Letter)),
                Times.Once);
        }

        [Test]
        public async Task Handle_TicketScanStartedNotification_EventPublished()
        {
            // Arrange
            var transactionHash = "d389jqwjfiok4mtktnwl_d3uifn3";
            var seat = new Seat { Number = 2, Letter = 'D' };
            var notification = new TicketScanStartedNotification(transactionHash, seat);

            // Act
            await _ticketScanRelayer.Handle(notification, default);

            // Assert
            _eventAggregator.Verify(callTo => callTo.Publish(It.Is<TicketScanAdded>(message => message.TransactionHash.EndsWith(transactionHash))), Times.Once);
        }
    }
}
