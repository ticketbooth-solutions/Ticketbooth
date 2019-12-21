using System;

namespace Ticketbooth.Scanner.Eventing.Args
{
    public class TicketCheckResultEventArgs : EventArgs
    {
        public TicketCheckResultEventArgs(string transactionHash, bool ownsTicket, string name)
        {
            TransactionHash = transactionHash;
            OwnsTicket = ownsTicket;
            Name = name;
        }

        public string TransactionHash { get; }

        public bool OwnsTicket { get; }

        public string Name { get; }
    }
}
