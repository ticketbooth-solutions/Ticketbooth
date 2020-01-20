namespace Ticketbooth.Scanner.Eventing.Events
{
    public class TicketScanAdded
    {
        public TicketScanAdded(string identifier)
        {
            Identifier = identifier;
        }

        public string Identifier { get; }
    }
}
