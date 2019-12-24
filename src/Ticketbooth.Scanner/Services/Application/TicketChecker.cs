using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Eventing.Args;
using Ticketbooth.Scanner.Services.Infrastructure;

namespace Ticketbooth.Scanner.Services.Application
{
    public class TicketChecker : ITicketChecker
    {
        public event EventHandler<TicketCheckEventArgs> OnCheckTicket;
        public event EventHandler<TicketCheckResultEventArgs> OnCheckTicketResult;

        private readonly ISmartContractService _smartContractService;
        private readonly ITicketService _ticketService;

        public TicketChecker(ISmartContractService smartContractService, ITicketService ticketService)
        {
            _smartContractService = smartContractService;
            _ticketService = ticketService;
        }

        public async Task<bool> PerformTicketCheckAsync(DigitalTicket ticket, CancellationToken cancellationToken)
        {
            var checkReservationResponse = await _ticketService.CheckReservationAsync(ticket.Seat, ticket.Address);
            if (checkReservationResponse is null || !checkReservationResponse.Success)
            {
                return false;
            }

            OnCheckTicket?.Invoke(this, new TicketCheckEventArgs(checkReservationResponse.TransactionId, ticket.Seat));
            PollResult(checkReservationResponse.TransactionId, cancellationToken);
            return true;
        }

        private async Task PollResult(string transactionHash, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var reservationResultReceipt = await _smartContractService.FetchReceiptAsync<TicketContract.ReservationQueryResult>(transactionHash);
                if (!(reservationResultReceipt is null))
                {
                    var reservationResult = reservationResultReceipt.ReturnValue;
                    var ownsTicket = reservationResult.OwnsTicket;
                    var name = reservationResult.CustomerIdentifier ?? string.Empty;
                    OnCheckTicketResult?.Invoke(this, new TicketCheckResultEventArgs(transactionHash, ownsTicket, name));
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            OnCheckTicketResult?.Invoke(this, new TicketCheckResultEventArgs(transactionHash, false, null, true));
        }
    }
}
