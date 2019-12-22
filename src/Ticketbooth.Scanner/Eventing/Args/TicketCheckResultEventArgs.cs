using System;

namespace Ticketbooth.Scanner.Eventing.Args
{
    public class TicketCheckResultEventArgs : EventArgs
    {
        public TicketCheckResultEventArgs(string transactionHash, bool ownsTicket, string name, bool faulted = false)
        {
            TransactionHash = transactionHash;
            OwnsTicket = ownsTicket;
            Name = name;
            Faulted = faulted;
        }

        public string TransactionHash { get; }

        public bool OwnsTicket { get; }

        public string Name { get; }

        public bool Faulted { get; }
    }
}
