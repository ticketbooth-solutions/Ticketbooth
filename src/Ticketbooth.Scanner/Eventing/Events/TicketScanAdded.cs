namespace Ticketbooth.Scanner.Eventing.Events
{
    public class TicketScanAdded
    {
        public TicketScanAdded(string transactionHash)
        {
            TransactionHash = transactionHash;
        }

        public string TransactionHash { get; }
    }
}
