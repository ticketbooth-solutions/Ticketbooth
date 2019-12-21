using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Eventing.Args;

namespace Ticketbooth.Scanner.Eventing
{
    public interface ITicketChecker
    {
        EventHandler<TicketCheckResultEventArgs> OnCheckTicketResult { get; set; }

        Task PerformTicketCheckAsync(DigitalTicket ticket, CancellationToken cancellationToken);
    }
}
