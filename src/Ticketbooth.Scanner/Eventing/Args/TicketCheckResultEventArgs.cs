using System;

namespace Ticketbooth.Scanner.Eventing.Args
{
    public class TicketCheckResultEventArgs : EventArgs
    {
        public TicketCheckResultEventArgs(TicketContract.Seat seat, bool ownsTicket, string name)
        {
            Seat = seat;
            OwnsTicket = ownsTicket;
            Name = name;
        }

        public TicketContract.Seat Seat { get; }

        public bool OwnsTicket { get; }

        public string Name { get; }
    }
}
