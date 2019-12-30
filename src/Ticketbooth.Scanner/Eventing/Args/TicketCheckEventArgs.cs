using System;

namespace Ticketbooth.Scanner.Eventing.Args
{
    public class TicketCheckRequestEventArgs : EventArgs
    {
        public TicketCheckRequestEventArgs(string transactionHash, TicketContract.Seat seat)
        {
            TransactionHash = transactionHash;
            Seat = seat;
        }

        public string TransactionHash { get; }

        public TicketContract.Seat Seat { get; }
    }
}
