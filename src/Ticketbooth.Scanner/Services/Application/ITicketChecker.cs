using System.Threading;
using System.Threading.Tasks;
using Ticketbooth.Scanner.Data.Dtos;

namespace Ticketbooth.Scanner.Services.Application
{
    public interface ITicketChecker
    {
        Task<string> PerformTicketCheckAsync(DigitalTicket ticket, CancellationToken cancellationToken);
    }
}
