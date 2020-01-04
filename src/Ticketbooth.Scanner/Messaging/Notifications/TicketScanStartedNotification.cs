using MediatR;
using static TicketContract;

namespace Ticketbooth.Scanner.Messaging.Notifications
{
    public class TicketScanStartedNotification : INotification
    {
        public TicketScanStartedNotification(string transactionHash, Seat seat)
        {
            TransactionHash = transactionHash;
            Seat = seat;
        }

        public string TransactionHash { get; }

        public Seat Seat { get; }
    }
}
