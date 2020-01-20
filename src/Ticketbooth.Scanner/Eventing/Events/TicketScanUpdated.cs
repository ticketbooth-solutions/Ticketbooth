namespace Ticketbooth.Scanner.Eventing.Events
{
    public class TicketScanUpdated
    {
        public TicketScanUpdated(string identifier)
        {
            Identifier = identifier;
        }

        public string Identifier { get; }
    }
}
