using System;

namespace Ticketbooth.Scanner.Eventing.Args
{
    public class TicketCheckEventArgs : EventArgs
    {
        public TicketCheckEventArgs(string transactionHash, TicketContract.Seat seat)
        {
            TransactionHash = transactionHash;
            Seat = seat;
        }

        public string TransactionHash { get; }

        public TicketContract.Seat Seat { get; }
    }
}
