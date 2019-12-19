using Stratis.SmartContracts;
using System.Threading;
using System.Threading.Tasks;
using static TicketContract;

namespace Ticketbooth.Scanner.ViewModels
{
    public interface IResultViewModel
    {
        public bool OwnsTicket { get; }

        public string Name { get; }

        public Seat Seat { get; }

        public Task PerformTicketCheckAsync(Seat seat, Address address, CancellationToken cancellationToken);
    }
}
