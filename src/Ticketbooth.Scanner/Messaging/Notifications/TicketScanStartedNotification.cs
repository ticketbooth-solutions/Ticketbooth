using MediatR;
using static TicketContract;

namespace Ticketbooth.Scanner.Messaging.Notifications
{
    public class TicketScanStartedNotification : INotification
    {
        public TicketScanStartedNotification(string identifier, Seat seat)
        {
            Identifier = identifier;
            Seat = seat;
        }

        public string Identifier { get; }

        public Seat Seat { get; }
    }
}
