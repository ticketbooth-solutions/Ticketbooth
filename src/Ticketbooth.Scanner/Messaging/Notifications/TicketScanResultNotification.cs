using MediatR;
using Ticketbooth.Scanner.Messaging.Data;

namespace Ticketbooth.Scanner.Messaging.Notifications
{
    public class TicketScanResultNotification : INotification
    {
        public TicketScanResultNotification(string identifier, TicketScanResult result)
        {
            Identifier = identifier;

            Result = result;
        }

        public string Identifier { get; }

        public TicketScanResult Result { get; }
    }
}
