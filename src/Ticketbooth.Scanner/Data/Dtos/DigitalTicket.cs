using Stratis.SmartContracts;

namespace Ticketbooth.Scanner.Data.Dtos
{
    public class DigitalTicket
    {
        public TicketContract.Seat Seat { get; set; }

        public Address Address { get; set; }
    }
}
