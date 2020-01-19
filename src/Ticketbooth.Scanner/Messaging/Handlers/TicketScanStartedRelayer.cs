using Easy.MessageHub;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Data.Models;
using Ticketbooth.Scanner.Eventing.Events;
using Ticketbooth.Scanner.Messaging.Notifications;

namespace Ticketbooth.Scanner.Messaging.Handlers
{
    public class TicketScanStartedRelayer : INotificationHandler<TicketScanStartedNotification>
    {
        private readonly ILogger<TicketScanStartedRelayer> _logger;
        private readonly IMessageHub _eventAggregator;
        private readonly ITicketRepository _ticketRepository;

        public TicketScanStartedRelayer(ILogger<TicketScanStartedRelayer> logger, IMessageHub eventAggregator, ITicketRepository ticketRepository)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;
            _ticketRepository = ticketRepository;
        }

        public Task Handle(TicketScanStartedNotification notification, CancellationToken cancellationToken)
        {
            var seat = new SeatModel(notification.Seat.Number, notification.Seat.Letter);
            _ticketRepository.Add(new TicketScanModel(notification.Identifier, seat));
            _eventAggregator.Publish(new TicketScanAdded(notification.Identifier));
            _logger.LogDebug($"Published {nameof(TicketScanAdded)} event for transaction {notification.Identifier}");
            return Task.CompletedTask;
        }
    }
}
