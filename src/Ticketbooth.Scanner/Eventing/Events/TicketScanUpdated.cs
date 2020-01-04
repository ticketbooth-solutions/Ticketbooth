namespace Ticketbooth.Scanner.Eventing.Events
{
    public class TicketScanUpdated
    {
        public TicketScanUpdated(string transactionHash)
        {
            TransactionHash = transactionHash;
        }

        public string TransactionHash { get; }
    }
}
