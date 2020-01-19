using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing.Events;
using Ticketbooth.Scanner.Messaging.Data;
using Ticketbooth.Scanner.Messaging.Handlers;
using Ticketbooth.Scanner.Messaging.Notifications;

namespace Ticketbooth.Scanner.Tests.Messaging.Handlers
{
    public class TicketScanResultRelayerTests
    {
        private Mock<IMessageHub> _eventAggregator;
        private Mock<ITicketRepository> _ticketRepository;
        private TicketScanResultRelayer _ticketScanRelayer;

        [SetUp]
        public void SetUp()
        {
            _eventAggregator = new Mock<IMessageHub>();
            _ticketRepository = new Mock<ITicketRepository>();
            _ticketScanRelayer = new TicketScanResultRelayer(Mock.Of<ILogger<TicketScanResultRelayer>>(), _eventAggregator.Object, _ticketRepository.Object);
        }

        [Test]
        public async Task Handle_TicketScanNotFound_NoEventPublished()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            var ticketScanResult = null as TicketScanResult;
            var ticketScanModel = null as TicketScanModel;
            var notification = new TicketScanResultNotification(identifier, ticketScanResult);

            _ticketRepository.Setup(callTo => callTo.Find(identifier)).Returns(ticketScanModel);

            // Act
            await _ticketScanRelayer.Handle(notification, default);

            // Assert
            _eventAggregator.Verify(callTo => callTo.Publish(It.IsAny<TicketScanUpdated>()), Times.Never);
        }

        [Test]
        public async Task Handle_TicketScanNullResult_EventPublished()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            var ticketScanResult = null as TicketScanResult;
            var ticketScanModel = new TicketScanModel(identifier, new SeatModel(5, 'D'));
            var notification = new TicketScanResultNotification(identifier, ticketScanResult);

            _ticketRepository.Setup(callTo => callTo.Find(identifier)).Returns(ticketScanModel);

            // Act
            await _ticketScanRelayer.Handle(notification, default);

            // Assert
            _eventAggregator.Verify(callTo => callTo.Publish(It.Is<TicketScanUpdated>(message => message.Identifier.Equals(identifier))), Times.Once);
        }

        [Test]
        public async Task Handle_TicketScanValueResult_EventPublished()
        {
            // Arrange
            var identifier = "09__blOoQm72n8Bf";
            var ticketScanResult = new TicketScanResult(true, "Benjamin Rich Swift");
            var ticketScanModel = new TicketScanModel(identifier, new SeatModel(5, 'D'));
            var notification = new TicketScanResultNotification(identifier, ticketScanResult);

            _ticketRepository.Setup(callTo => callTo.Find(identifier)).Returns(ticketScanModel);

            // Act
            await _ticketScanRelayer.Handle(notification, default);

            // Assert
            _eventAggregator.Verify(callTo => callTo.Publish(It.Is<TicketScanUpdated>(message => message.Identifier.Equals(identifier))), Times.Once);
        }
    }
}
