using MediatR;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Handlers;
using Ticketbooth.Scanner.Messaging.Notifications;
using Ticketbooth.Scanner.Services.Infrastructure;
using static TicketContract;

namespace Ticketbooth.Scanner.Tests.Messaging.Handlers
{
    public class TicketScanPollerTests
    {
        private Mock<IMediator> _mediator;
        private Mock<ISmartContractService> _smartContractService;
        private TicketScanPoller _ticketScanPoller;

        [SetUp]
        public void SetUp()
        {
            _mediator = new Mock<IMediator>();
            _smartContractService = new Mock<ISmartContractService>();
            _ticketScanPoller = new TicketScanPoller(_mediator.Object, _smartContractService.Object);
        }

        [Test]
        public async Task Handle_CancellationTokenCancelled_PublishesRequestNullResult()
        {
            // Arrange
            var transactionHash = "jw829jf389g23";
            var notification = new TicketScanStartedNotification(transactionHash, new Seat { Number = 3, Letter = 'D' });
            var cancellationToken = new CancellationToken(true);

            // Act
            await _ticketScanPoller.Handle(notification, cancellationToken);

            // Assert
            _mediator.Verify(callTo => callTo.Publish(It.Is<TicketScanResultNotification>(
                request => request.TransactionHash.Equals(transactionHash) && request.Result == null), default), Times.Once);
        }

        [Test]
        public async Task Handle_FetchReceiptAsyncReturnsNull_PollsEndpoint()
        {
            // Arrange
            var transactionHash = "jw829jf389g23";
            var notification = new TicketScanStartedNotification(transactionHash, new Seat { Number = 3, Letter = 'D' });
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var cancellationToken = cancellationTokenSource.Token;
            var receipt = null as Receipt<ReservationQueryResult>;

            _smartContractService.Setup(callTo => callTo.FetchReceiptAsync<ReservationQueryResult>(transactionHash)).Returns(Task.FromResult(receipt));

            // Act
            await _ticketScanPoller.Handle(notification, cancellationToken);

            // Assert
            _smartContractService.Verify(callTo => callTo.FetchReceiptAsync<ReservationQueryResult>(transactionHash), Times.AtLeast(2));
        }

        [Test]
        public async Task Handle_FetchReceiptAsyncReturnsValue_PublishesRequestWithResultValue()
        {
            // Arrange
            var ownsTicket = true;
            var customerName = "Benajmin Rich Swift";

            var transactionHash = "jw829jf389g23";
            var notification = new TicketScanStartedNotification(transactionHash, new Seat { Number = 3, Letter = 'D' });
            var cancellationToken = default(CancellationToken);
            var receipt = new Receipt<ReservationQueryResult>
            {
                ReturnValue = new ReservationQueryResult
                {
                    OwnsTicket = ownsTicket,
                    CustomerIdentifier = customerName
                }
            };

            _smartContractService.Setup(callTo => callTo.FetchReceiptAsync<ReservationQueryResult>(transactionHash)).Returns(Task.FromResult(receipt));

            // Act
            await _ticketScanPoller.Handle(notification, cancellationToken);

            // Assert
            _mediator.Verify(callTo => callTo.Publish(It.Is<TicketScanResultNotification>(
                request => request.TransactionHash.Equals(transactionHash)
                    && request.Result.OwnsTicket == ownsTicket && request.Result.Name.Equals(customerName)), default), Times.Once);
        }
    }
}
