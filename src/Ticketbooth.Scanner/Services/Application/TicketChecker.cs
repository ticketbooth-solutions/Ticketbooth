using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Services.Infrastructure;

namespace Ticketbooth.Scanner.Services.Application
{
    public class TicketChecker : ITicketChecker
    {
        private readonly ITicketService _ticketService;

        public TicketChecker(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        public async Task<string> PerformTicketCheckAsync(DigitalTicket ticket)
        {
            var checkReservationResponse = await _ticketService.CheckReservationAsync(ticket.Seat, ticket.Address);
            if (checkReservationResponse is null || !checkReservationResponse.Success)
            {
                return null;
            }

            return checkReservationResponse.TransactionId;
        }
    }
}
