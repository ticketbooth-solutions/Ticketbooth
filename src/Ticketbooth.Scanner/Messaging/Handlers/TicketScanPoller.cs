using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Messaging.Data;
using Ticketbooth.Scanner.Messaging.Notifications;
using Ticketbooth.Scanner.Services.Infrastructure;

namespace Ticketbooth.Scanner.Messaging.Handlers
{
    public class TicketScanPoller : INotificationHandler<TicketScanStartedNotification>
    {
        private readonly IMediator _mediator;
        private readonly ISmartContractService _smartContractService;

        public TicketScanPoller(IMediator mediator, ISmartContractService smartContractService)
        {
            _mediator = mediator;
            _smartContractService = smartContractService;
        }

        public async Task Handle(TicketScanStartedNotification ticketScan, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var reservationResultReceipt = await _smartContractService.FetchReceiptAsync<TicketContract.ReservationQueryResult>(ticketScan.TransactionHash);
                if (!(reservationResultReceipt is null))
                {
                    var reservationResult = reservationResultReceipt.ReturnValue;
                    var ownsTicket = reservationResult.OwnsTicket;
                    var name = reservationResult.CustomerIdentifier ?? string.Empty;
                    await _mediator.Publish(new TicketScanResultNotification(ticketScan.TransactionHash, new TicketScanResult(ownsTicket, name)));
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            await _mediator.Publish(new TicketScanResultNotification(ticketScan.TransactionHash, null));
        }
    }
}
