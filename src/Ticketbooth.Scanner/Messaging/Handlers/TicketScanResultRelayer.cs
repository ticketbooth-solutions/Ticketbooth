using Easy.MessageHub;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data;
using Ticketbooth.Scanner.Eventing.Events;
using Ticketbooth.Scanner.Messaging.Notifications;

namespace Ticketbooth.Scanner.Messaging.Handlers
{
    public class TicketScanResultRelayer : INotificationHandler<TicketScanResultNotification>
    {
        private readonly ILogger<TicketScanResultRelayer> _logger;
        private readonly IMessageHub _eventAggregator;
        private readonly ITicketRepository _ticketRepository;

        public TicketScanResultRelayer(ILogger<TicketScanResultRelayer> logger, IMessageHub eventAggregator, ITicketRepository ticketRepository)
        {
            _logger = logger;
            _eventAggregator = eventAggregator;
            _ticketRepository = ticketRepository;
        }

        public async Task Handle(TicketScanResultNotification notification, CancellationToken cancellationToken)
        {
            var ticketScan = _ticketRepository.Find(notification.Identifier);
            if (ticketScan is null)
            {
                return;
            }

            if (notification.Result is null)
            {
                ticketScan.SetFaulted();
            }
            else
            {
                ticketScan.SetScanResult(notification.Result.OwnsTicket, notification.Result.Name);
            }

            _eventAggregator.Publish(new TicketScanUpdated(ticketScan.Identifier));
            _logger.LogDebug($"Published {nameof(TicketScanUpdated)} event for transaction {ticketScan.Identifier}");
        }
    }
}
