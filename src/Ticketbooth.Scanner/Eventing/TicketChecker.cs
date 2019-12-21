using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services;

namespace Ticketbooth.Scanner.Eventing
{
    public class TicketChecker : ITicketChecker
    {
        private readonly ISmartContractService _smartContractService;
        private readonly ITicketService _ticketService;

        public TicketChecker(ISmartContractService smartContractService, ITicketService ticketService)
        {
            _smartContractService = smartContractService;
            _ticketService = ticketService;
        }

        public EventHandler<TicketCheckResultEventArgs> OnCheckTicketResult { get; set; }

        public async Task PerformTicketCheckAsync(DigitalTicket ticket, CancellationToken cancellationToken)
        {
            var checkReservationResponse = await _ticketService.CheckReservationAsync(ticket.Seat, ticket.Address);
            if (checkReservationResponse is null || !checkReservationResponse.Success)
            {
                OnCheckTicketResult?.Invoke(this, new TicketCheckResultEventArgs(ticket.Seat, false, null));
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var reservationResultReceipt = await _smartContractService.FetchReceiptAsync<TicketContract.ReservationQueryResult>(checkReservationResponse.TransactionId);
                if (!(reservationResultReceipt is null))
                {
                    var reservationResult = reservationResultReceipt.ReturnValue;
                    OnCheckTicketResult?.Invoke(this, new TicketCheckResultEventArgs(ticket.Seat, reservationResult.OwnsTicket, reservationResult.CustomerIdentifier));
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
