using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Messaging.Data;
using static TicketContract;

namespace Ticketbooth.Scanner.Services.Application
{
    public interface ITicketChecker
    {
        TicketScanResult CheckTicket(DigitalTicket ticket, Ticket actualTicket);
    }
}
