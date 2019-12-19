using Stratis.SmartContracts;
using System;
using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Eventing.Args;

namespace Ticketbooth.Scanner.Eventing
{
    public interface ITicketChecker
    {
        EventHandler<TicketCheckResultEventArgs> OnCheckTicketResult { get; set; }

        Task PerformTicketCheckAsync(TicketContract.Seat seat, Address address, CancellationToken cancellationToken);
    }
}
