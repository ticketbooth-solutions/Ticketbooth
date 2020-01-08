using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Services.Infrastructure;

namespace Ticketbooth.Scanner.Services.Application
{
    public class TicketChecker : ITicketChecker
    {
        private readonly ILogger<TicketChecker> _logger;
        private readonly ITicketService _ticketService;

        public TicketChecker(ILogger<TicketChecker> logger, ITicketService ticketService)
        {
            _logger = logger;
            _ticketService = ticketService;
        }

        public async Task<string> PerformTicketCheckAsync(DigitalTicket ticket, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var checkReservationResponse = await _ticketService.CheckReservationAsync(ticket.Seat, ticket.Address);
                if (checkReservationResponse is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(.5));
                }
                else if (checkReservationResponse.Success)
                {
                    return checkReservationResponse.TransactionId;
                }
                else
                {
                    _logger.LogError($"Error when building smart contract call: {checkReservationResponse.Message}");
                    return null;
                }
            }

            _logger.LogError($"Ticket check timed out for seat {ticket.Seat.Number}{ticket.Seat.Letter}.");
            return null;
        }
    }
}
