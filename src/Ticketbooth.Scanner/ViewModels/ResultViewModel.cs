using Stratis.SmartContracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Services;
using static TicketContract;

namespace Ticketbooth.Scanner.ViewModels
{
    public class ResultViewModel : IResultViewModel
    {
        private readonly ISmartContractService _smartContractService;
        private readonly ITicketService _ticketService;

        public bool OwnsTicket { get; private set; }

        public string Name { get; private set; }

        public Seat Seat { get; private set; }

        public ResultViewModel(ISmartContractService smartContractService, ITicketService ticketService)
        {
            _smartContractService = smartContractService;
            _ticketService = ticketService;
        }

        public async Task PerformTicketCheckAsync(Seat seat, Address address, CancellationToken cancellationToken)
        {
            Seat = seat;

            var checkReservationResponse = await _ticketService.CheckReservationAsync(seat, address);
            if (checkReservationResponse is null || !checkReservationResponse.Success)
            {
                OwnsTicket = false;
                Name = string.Empty;
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var reservationResultReceipt = await _smartContractService.FetchReceiptAsync<ReservationQueryResult>(checkReservationResponse.TransactionId);
                if (!(reservationResultReceipt is null))
                {
                    var reservationResult = reservationResultReceipt.ReturnValue;
                    OwnsTicket = reservationResult.OwnsTicket;
                    Name = reservationResult.CustomerIdentifier ?? string.Empty;
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}
