using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;
using Ticketbooth.Scanner.Eventing.Args;

namespace Ticketbooth.Scanner.Services.Application
{
    public interface ITicketChecker
    {
        event EventHandler<TicketCheckEventArgs> OnCheckTicket;

        event EventHandler<TicketCheckResultEventArgs> OnCheckTicketResult;

        Task<bool> PerformTicketCheckAsync(DigitalTicket ticket, CancellationToken cancellationToken);
    }
}
