using static TicketContract;

namespace Ticketbooth.Scanner.Data.Dtos
{
    public class DigitalTicket
    {
        public Seat Seat { get; set; }

        public string Secret { get; set; }

        public byte[] SecretKey { get; set; }

        public byte[] SecretIV { get; set; }

        public byte[] NameKey { get; set; }

        public byte[] NameIV { get; set; }
    }
}
