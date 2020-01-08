namespace Ticketbooth.Scanner.Messaging.Data
{
    public class TicketScanResult
    {
        public TicketScanResult(bool ownsTicket, string name)
        {
            OwnsTicket = ownsTicket;
            Name = name;
        }

        public bool OwnsTicket { get; }

        public string Name { get; }
    }
}
