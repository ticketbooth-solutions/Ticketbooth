using MediatR;
using Ticketbooth.Scanner.Messaging.Data;

namespace Ticketbooth.Scanner.Messaging.Notifications
{
    public class TicketScanResultNotification : INotification
    {
        public TicketScanResultNotification(string transactionHash, TicketScanResult result)
        {
            TransactionHash = transactionHash;
            Result = result;
        }

        public string TransactionHash { get; }

        public TicketScanResult Result { get; }
    }
}
